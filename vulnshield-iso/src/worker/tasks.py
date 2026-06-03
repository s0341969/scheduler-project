import logging
import asyncio
import datetime
import json
from pathlib import Path

from src.worker.celery_app import celery_app
from src.services.nmap_service import NmapService
from src.services.nuclei_service import NucleiService
from src.models.database import async_session
from src.models.scan import ScanTask, ScanStatus
from src.models.asset import Asset
from src.services.scan_processing import get_or_create_vulnerability, upsert_finding

logger = logging.getLogger(__name__)

async def run_sync_scan(target: str):
    # 1. Nmap Discovery
    nmap = NmapService()
    nmap_results = await nmap.scan(target)
    
    # 2. Nuclei Scan
    nuclei = NucleiService()
    nuclei_results = await nuclei.scan(target)
    
    return nmap_results, nuclei_results

@celery_app.task(name='tasks.execute_scan')
def execute_scan(task_id: int):
    return asyncio.run(_execute_scan_logic(task_id))

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
            nmap_res, nuclei_res = await run_sync_scan(asset.target)
            raw_dir = Path('/tmp/vulnshield-scans')
            raw_dir.mkdir(parents=True, exist_ok=True)
            raw_output_path = raw_dir / f'scan-{task.id}.json'
            raw_output_path.write_text(
                json.dumps({'nmap': nmap_res, 'nuclei': nuclei_res}, ensure_ascii=False, indent=2),
                encoding='utf-8',
            )
            task.raw_output_path = str(raw_output_path)
            
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

