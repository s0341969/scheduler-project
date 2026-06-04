import logging
import asyncio
import datetime
import json
from pathlib import Path
from sqlalchemy import select
from sqlalchemy.orm import selectinload

from src.worker.celery_app import celery_app
from src.services.nmap_service import NmapService
from src.services.nuclei_service import NucleiService
from src.models.database import async_session
from src.models.credential import Credential
from src.models.scan import ScanSchedule, ScanTask, ScanStatus
from src.models.asset import Asset
from src.models.user import User
from src.services.asset_inventory import ensure_asset_status, ensure_scan_profile, ensure_template_key
from src.services.credential_service import build_credential_runtime_context
from src.services.scan_catalog import (
    build_redacted_credential_metadata,
    build_scan_execution_plan,
    credential_kind_supported_by_profile,
    profile_requires_credential,
)
from src.services.schedule_service import calculate_next_run_at, parse_weekdays
from src.services.scan_processing import get_or_create_vulnerability, upsert_finding
from src.services.scan_summary import summarize_scan_results

logger = logging.getLogger(__name__)
_ = User

async def run_sync_scan(target: str, scan_config: dict, credential_context: dict | None = None):
    nmap = NmapService()
    nmap_results = []
    if scan_config['engines']['nmap']:
        script_name = None
        script_args = None
        if credential_context and credential_context.get('kind') == 'SNMPv2c':
            script_name = 'snmp-info'
            script_args = f"snmpcommunity={credential_context['primary_secret']}"
        nmap_results = await nmap.scan(
            target,
            scan_args=scan_config.get('nmap_args') or ['-sV', '-T4'],
            script_name=script_name,
            script_args=script_args,
            timeout=min(int(scan_config.get('max_duration_seconds', 600)), 1200),
        )

    nuclei = NucleiService()
    nuclei_results = []
    if scan_config['engines']['nuclei']:
        nuclei_results = await nuclei.scan(
            target,
            scan_tags=scan_config.get('nuclei_tags') or [],
            timeout=min(max(int(scan_config.get('max_duration_seconds', 600)), 300), 1500),
        )
    
    return nmap_results, nuclei_results

@celery_app.task(name='tasks.execute_scan')
def execute_scan(task_id: int):
    return asyncio.run(_execute_scan_logic(task_id))

@celery_app.task(name='tasks.sync_scan_schedules')
def sync_scan_schedules():
    return asyncio.run(_sync_scan_schedules_logic())

async def _execute_scan_logic(task_id: int):
    async with async_session() as session:
        # Update task to Running
        task = await session.get(ScanTask, task_id)
        if not task:
            return 'Task not found'
        
        task.status = ScanStatus.RUNNING
        task.started_at = datetime.datetime.utcnow()
        await session.commit()

        try:
            asset = await session.get(Asset, task.asset_id)
            credential = await session.get(Credential, task.credential_id) if task.credential_id else None
            resolved_profile = ensure_scan_profile(task.scan_profile or getattr(asset, 'default_scan_profile', None))
            resolved_template = ensure_template_key(task.device_template or getattr(asset, 'template_key', None), asset.device_type)
            credential_context = build_credential_runtime_context(credential)

            if credential is not None and not credential.is_active:
                raise ValueError('此掃描任務綁定的 credential 已停用，無法繼續執行')

            if profile_requires_credential(resolved_profile):
                if credential is None or credential_context is None:
                    raise ValueError('此掃描模式需要 credential，但任務未綁定可用憑證')
                if not credential_kind_supported_by_profile(resolved_profile, credential.kind):
                    raise ValueError('credential 類型與掃描模式不相容')

            scan_config = build_scan_execution_plan(resolved_profile, resolved_template)
            scan_config['credential'] = build_redacted_credential_metadata(credential_context)

            task.scan_profile = resolved_profile
            task.device_template = resolved_template
            task.scan_config = scan_config
            if credential is not None:
                credential.last_used_at = datetime.datetime.utcnow()
            await session.commit()

            nmap_res, nuclei_res = await run_sync_scan(asset.target, scan_config, credential_context)
            raw_dir = Path('/tmp/vulnshield-scans')
            raw_dir.mkdir(parents=True, exist_ok=True)
            raw_output_path = raw_dir / f'scan-{task.id}.json'
            raw_output_path.write_text(
                json.dumps({'config': scan_config, 'nmap': nmap_res, 'nuclei': nuclei_res}, ensure_ascii=False, indent=2),
                encoding='utf-8',
            )
            task.raw_output_path = str(raw_output_path)
            task.scan_summary = summarize_scan_results(
                nmap_res,
                nuclei_res,
                profile_key=resolved_profile,
                device_template_key=resolved_template,
                credential_metadata=build_redacted_credential_metadata(credential_context),
            )
            
            for item in nuclei_res:
                vulnerability = await get_or_create_vulnerability(session, item)
                await upsert_finding(session, asset, vulnerability)
            
            task.status = ScanStatus.COMPLETED
            task.finished_at = datetime.datetime.utcnow()
            await session.commit()
            return 'Scan completed successfully'
            
        except Exception as e:
            logger.error(f'Task {task_id} failed: {str(e)}')
            task.status = ScanStatus.FAILED
            task.error_message = str(e)
            await session.commit()
            return f'Scan failed: {str(e)}'


async def _sync_scan_schedules_logic():
    now_utc = datetime.datetime.now(datetime.timezone.utc)
    created_tasks = 0
    pending_task_ids: list[int] = []

    async with async_session() as session:
        result = await session.execute(
            select(ScanSchedule)
            .options(
                selectinload(ScanSchedule.asset),
                selectinload(ScanSchedule.credential),
            )
            .where(
                ScanSchedule.is_active.is_(True),
                ScanSchedule.next_run_at.is_not(None),
                ScanSchedule.next_run_at <= now_utc,
            )
            .order_by(ScanSchedule.next_run_at.asc(), ScanSchedule.id.asc())
        )
        schedules = result.scalars().all()

        for schedule in schedules:
            asset = schedule.asset
            credential = schedule.credential
            schedule.last_run_at = now_utc
            schedule.last_error = None

            try:
                if asset is None:
                    raise ValueError('排程綁定的設備不存在')
                if ensure_asset_status(asset.status) == 'Retired':
                    raise ValueError('設備已退役，排程已略過')
                if credential is not None and not credential.is_active:
                    raise ValueError('排程綁定的 credential 已停用')
                if profile_requires_credential(schedule.scan_profile):
                    if credential is None:
                        raise ValueError('此排程模式需要 credential，但目前未綁定')
                    if not credential_kind_supported_by_profile(schedule.scan_profile, credential.kind):
                        raise ValueError('排程綁定的 credential 類型與掃描模式不相容')

                next_run_at = calculate_next_run_at(
                    cadence=schedule.cadence,
                    timezone_name=schedule.timezone,
                    run_hour=schedule.run_hour,
                    run_minute=schedule.run_minute,
                    weekdays=parse_weekdays(schedule.weekdays),
                    cron_expr=schedule.cron_expr,
                    base_time=now_utc + datetime.timedelta(minutes=1),
                )
                task = ScanTask(
                    asset_id=schedule.asset_id,
                    scan_profile=ensure_scan_profile(schedule.scan_profile),
                    device_template=ensure_template_key(schedule.device_template, asset.device_type),
                    credential_id=schedule.credential_id,
                    schedule_id=schedule.id,
                )
                session.add(task)
                await session.flush()

                schedule.last_task_id = task.id
                schedule.next_run_at = next_run_at
                pending_task_ids.append(task.id)
                created_tasks += 1
            except Exception as exc:
                logger.error('Schedule %s sync failed: %s', schedule.id, str(exc))
                schedule.last_error = str(exc)
                schedule.next_run_at = calculate_next_run_at(
                    cadence=schedule.cadence,
                    timezone_name=schedule.timezone,
                    run_hour=schedule.run_hour,
                    run_minute=schedule.run_minute,
                    weekdays=parse_weekdays(schedule.weekdays),
                    cron_expr=schedule.cron_expr,
                    base_time=now_utc + datetime.timedelta(minutes=1),
                )

        await session.commit()

    for task_id in pending_task_ids:
        execute_scan.delay(task_id)

    return f'scheduled {created_tasks} tasks'

