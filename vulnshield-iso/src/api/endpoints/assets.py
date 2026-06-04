from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from src.api.deps import get_current_user, get_db, require_roles
from src.models.asset import Asset
from src.models.credential import Credential
from src.models.scan import ScanSchedule, ScanTask
from src.models.user import User, UserRole
from src.models.vulnerability import Finding
from src.schemas.asset import AssetCreate, AssetResponse, AssetUpdate
from src.schemas.schedule import ScanScheduleCreate, ScanScheduleResponse
from src.schemas.scan import AssetScanRequest, ScanResponse
from src.schemas.vulnerability import FindingDetailResponse
from src.services.audit_service import add_audit_log
from src.services.asset_inventory import (
    asset_to_response,
    build_asset_summary_map,
    ensure_asset_status,
    ensure_scan_profile,
    ensure_template_key,
    normalize_tags,
)
from src.services.scan_catalog import recommended_profile_for_template, recommended_template_for_device_type
from src.services.scan_catalog import credential_kind_supported_by_profile, profile_requires_credential
from src.services.schedule_service import (
    calculate_next_run_at,
    normalize_schedule_payload_values,
    schedule_to_response,
    serialize_weekdays,
)
from src.services.scan_summary import normalize_scan_summary_payload
from src.worker.tasks import execute_scan


def serialize_asset_scan(task: ScanTask, asset: Asset | None = None) -> ScanResponse:
    resolved_asset = asset or task.asset
    resolved_credential = task.credential
    return ScanResponse(
        id=task.id,
        asset_id=task.asset_id,
        asset_name=resolved_asset.name if resolved_asset else None,
        asset_target=resolved_asset.target if resolved_asset else None,
        asset_device_type=resolved_asset.device_type if resolved_asset else None,
        schedule_id=task.schedule_id,
        scan_profile=ensure_scan_profile(task.scan_profile),
        device_template=ensure_template_key(task.device_template, resolved_asset.device_type if resolved_asset else None),
        credential_id=task.credential_id,
        credential_name=resolved_credential.name if resolved_credential else None,
        credential_kind=resolved_credential.kind if resolved_credential else None,
        status=task.status,
        started_at=task.started_at,
        finished_at=task.finished_at,
        raw_output_path=task.raw_output_path,
        scan_summary=normalize_scan_summary_payload(
            task.scan_summary,
            profile_key=ensure_scan_profile(task.scan_profile),
            device_template_key=ensure_template_key(task.device_template, resolved_asset.device_type if resolved_asset else None),
        ),
        scan_config=task.scan_config,
        error_message=task.error_message,
    )


router = APIRouter(prefix='/assets', tags=['assets'])


async def get_accessible_asset(asset_id: int, db: AsyncSession, current_user: User) -> Asset:
    asset = await db.get(Asset, asset_id)
    if asset is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到設備')

    if current_user.role == UserRole.ANALYST and asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法操作他人的設備')

    return asset


async def get_accessible_credential(
    credential_id: int | None,
    db: AsyncSession,
    current_user: User,
) -> Credential | None:
    if credential_id is None:
        return None
    credential = await db.get(Credential, credential_id)
    if credential is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到 credential')
    if current_user.role == UserRole.ANALYST and credential.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法使用他人的 credential')
    if not credential.is_active:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail='此 credential 已停用，無法綁定或執行掃描')
    return credential


def resolve_schedule_payload(
    *,
    asset: Asset,
    payload: ScanScheduleCreate,
) -> dict:
    normalized_profile = ensure_scan_profile(payload.scan_profile or getattr(asset, 'default_scan_profile', None))
    normalized_template = ensure_template_key(payload.device_template or getattr(asset, 'template_key', None), asset.device_type)
    normalized_schedule = normalize_schedule_payload_values(
        cadence=payload.cadence,
        timezone_name=payload.timezone,
        weekdays=payload.weekdays,
        run_hour=payload.run_hour,
        run_minute=payload.run_minute,
        cron_expr=payload.cron_expr,
    )
    return {
        'name': payload.name,
        'cadence': normalized_schedule['cadence'],
        'timezone': normalized_schedule['timezone'],
        'weekdays': serialize_weekdays(normalized_schedule['weekdays']),
        'run_hour': normalized_schedule['run_hour'],
        'run_minute': normalized_schedule['run_minute'],
        'cron_expr': normalized_schedule['cron_expr'],
        'scan_profile': normalized_profile,
        'device_template': normalized_template,
        'is_active': payload.is_active,
    }


@router.post('', response_model=AssetResponse, status_code=status.HTTP_201_CREATED)
async def create_asset(
    asset: AssetCreate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    if current_user.role == UserRole.ANALYST and current_user.id != asset.owner_id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='Analyst 只能建立自己的設備')

    payload = asset.model_dump()
    payload['device_type'] = payload['device_type'].value
    payload['default_scan_profile'] = ensure_scan_profile(payload.get('default_scan_profile'))
    payload['template_key'] = ensure_template_key(payload.get('template_key'), payload['device_type'])
    payload['status'] = ensure_asset_status(payload.get('status'))
    if payload['default_scan_profile'] == 'standard' and asset.template_key is None:
        payload['default_scan_profile'] = recommended_profile_for_template(payload['template_key'])
    payload['tags'] = normalize_tags(payload['tags'])
    credential = await get_accessible_credential(payload.get('default_credential_id'), db, current_user)
    payload['default_credential_id'] = credential.id if credential else None

    new_asset = Asset(**payload)
    db.add(new_asset)
    await db.flush()
    add_audit_log(
        db,
        user=current_user,
        action='CREATE_ASSET',
        entity_type='Asset',
        entity_id=new_asset.id,
        payload={
            'name': new_asset.name,
            'target': new_asset.target,
            'device_type': new_asset.device_type,
            'default_scan_profile': new_asset.default_scan_profile,
            'template_key': new_asset.template_key,
            'default_credential_id': new_asset.default_credential_id,
        },
    )
    await db.commit()
    await db.refresh(new_asset)
    await db.refresh(new_asset, attribute_names=['default_credential'])

    summary_map = await build_asset_summary_map(db, [new_asset])
    return asset_to_response(new_asset, summary_map.get(new_asset.id))


@router.get('', response_model=List[AssetResponse])
async def list_assets(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user),
):
    statement = select(Asset).options(selectinload(Asset.default_credential))
    if current_user.role == UserRole.ANALYST:
        statement = statement.where(Asset.owner_id == current_user.id)

    result = await db.execute(statement.order_by(Asset.id.asc()))
    assets = result.scalars().all()
    summary_map = await build_asset_summary_map(db, assets)
    return [asset_to_response(asset, summary_map.get(asset.id)) for asset in assets]


@router.get('/{asset_id}', response_model=AssetResponse)
async def get_asset(
    asset_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    await db.refresh(asset, attribute_names=['default_credential'])
    summary_map = await build_asset_summary_map(db, [asset])
    return asset_to_response(asset, summary_map.get(asset.id))


@router.patch('/{asset_id}', response_model=AssetResponse)
async def update_asset(
    asset_id: int,
    payload: AssetUpdate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    original_values = {
        'name': asset.name,
        'target': asset.target,
        'criticality': asset.criticality,
        'env': asset.env.value if hasattr(asset.env, 'value') else asset.env,
        'device_type': asset.device_type,
        'default_scan_profile': asset.default_scan_profile,
        'template_key': asset.template_key,
        'default_credential_id': asset.default_credential_id,
        'location': asset.location,
        'tags': asset.tags,
        'notes': asset.notes,
        'status': asset.status,
    }

    values = payload.model_dump(exclude_unset=True)
    if 'device_type' in values and values['device_type'] is not None:
        values['device_type'] = values['device_type'].value
    if 'default_scan_profile' in values and values['default_scan_profile'] is not None:
        values['default_scan_profile'] = ensure_scan_profile(values['default_scan_profile'])
    if 'status' in values and values['status'] is not None:
        values['status'] = ensure_asset_status(values['status'])
    if 'template_key' in values:
        values['template_key'] = ensure_template_key(values['template_key'], values.get('device_type') or asset.device_type)
    elif 'device_type' in values:
        values['template_key'] = recommended_template_for_device_type(values['device_type'])
    if 'default_credential_id' in values:
        credential = await get_accessible_credential(values['default_credential_id'], db, current_user)
        values['default_credential_id'] = credential.id if credential else None
    if 'tags' in values:
        values['tags'] = normalize_tags(values['tags'])

    for field, value in values.items():
        setattr(asset, field, value)

    changed_fields = {}
    for field, old_value in original_values.items():
        new_value = getattr(asset, field)
        comparable_new = new_value.value if hasattr(new_value, 'value') else new_value
        if old_value != comparable_new:
            changed_fields[field] = {'old': old_value, 'new': comparable_new}
    if changed_fields:
        add_audit_log(
            db,
            user=current_user,
            action='UPDATE_ASSET',
            entity_type='Asset',
            entity_id=asset.id,
            payload=changed_fields,
        )

    await db.commit()
    await db.refresh(asset)
    await db.refresh(asset, attribute_names=['default_credential'])
    summary_map = await build_asset_summary_map(db, [asset])
    return asset_to_response(asset, summary_map.get(asset.id))


@router.post('/{asset_id}/scan', response_model=ScanResponse, status_code=status.HTTP_202_ACCEPTED)
async def trigger_asset_scan(
    asset_id: int,
    payload: AssetScanRequest | None = None,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    await db.refresh(asset, attribute_names=['default_credential'])
    requested_profile = ensure_scan_profile(payload.scan_profile if payload else getattr(asset, 'default_scan_profile', None))
    requested_template = ensure_template_key(
        payload.device_template if payload else getattr(asset, 'template_key', None),
        asset.device_type,
    )
    if ensure_asset_status(asset.status) == 'Retired':
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail='退役設備不可執行掃描，請先將設備狀態改回運作中或維護中')
    credential = await get_accessible_credential(
        payload.credential_id if payload and payload.credential_id is not None else getattr(asset, 'default_credential_id', None),
        db,
        current_user,
    )
    if profile_requires_credential(requested_profile):
        if credential is None:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='此掃描模式需要綁定 credential')
        if not credential_kind_supported_by_profile(requested_profile, credential.kind):
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='credential 類型與掃描模式不相容')
    new_task = ScanTask(
        asset_id=asset.id,
        scan_profile=requested_profile,
        device_template=requested_template,
        credential_id=credential.id if credential else None,
    )
    db.add(new_task)
    await db.commit()
    await db.refresh(new_task)
    if credential is not None:
        await db.refresh(new_task, attribute_names=['credential'])

    execute_scan.delay(new_task.id)
    return serialize_asset_scan(new_task, asset)


@router.get('/{asset_id}/scans', response_model=List[ScanResponse])
async def list_asset_scans(
    asset_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    result = await db.execute(
        select(ScanTask)
        .options(selectinload(ScanTask.credential))
        .where(ScanTask.asset_id == asset.id)
        .order_by(ScanTask.id.desc())
    )
    return [serialize_asset_scan(task, asset) for task in result.scalars().all()]


@router.get('/{asset_id}/schedules', response_model=List[ScanScheduleResponse])
async def list_asset_schedules(
    asset_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    result = await db.execute(
        select(ScanSchedule)
        .options(selectinload(ScanSchedule.asset), selectinload(ScanSchedule.credential))
        .where(ScanSchedule.asset_id == asset.id)
        .order_by(ScanSchedule.id.desc())
    )
    return [schedule_to_response(schedule) for schedule in result.scalars().all()]


@router.post('/{asset_id}/schedules', response_model=ScanScheduleResponse, status_code=status.HTTP_201_CREATED)
async def create_asset_schedule(
    asset_id: int,
    payload: ScanScheduleCreate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    if ensure_asset_status(asset.status) == 'Retired':
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail='退役設備不可建立排程掃描')

    credential = await get_accessible_credential(
        payload.credential_id if payload.credential_id is not None else getattr(asset, 'default_credential_id', None),
        db,
        current_user,
    )
    resolved_payload = resolve_schedule_payload(asset=asset, payload=payload)

    if profile_requires_credential(resolved_payload['scan_profile']):
        if credential is None:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='此排程模式需要綁定 credential')
        if not credential_kind_supported_by_profile(resolved_payload['scan_profile'], credential.kind):
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='credential 類型與排程掃描模式不相容')

    next_run_at = (
        calculate_next_run_at(
            cadence=resolved_payload['cadence'],
            timezone_name=resolved_payload['timezone'],
            run_hour=resolved_payload['run_hour'],
            run_minute=resolved_payload['run_minute'],
            weekdays=payload.weekdays,
            cron_expr=resolved_payload['cron_expr'],
        )
        if resolved_payload['is_active']
        else None
    )
    schedule = ScanSchedule(
        asset_id=asset.id,
        credential_id=credential.id if credential else None,
        next_run_at=next_run_at,
        **resolved_payload,
    )
    db.add(schedule)
    await db.flush()
    add_audit_log(
        db,
        user=current_user,
        action='CREATE_SCHEDULE',
        entity_type='ScanSchedule',
        entity_id=schedule.id,
        payload={'asset_id': asset.id, 'cadence': schedule.cadence, 'scan_profile': schedule.scan_profile},
    )
    await db.commit()
    await db.refresh(schedule, attribute_names=['asset', 'credential'])
    return schedule_to_response(schedule)


@router.get('/{asset_id}/findings', response_model=List[FindingDetailResponse])
async def list_asset_findings(
    asset_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    result = await db.execute(
        select(Finding)
        .options(selectinload(Finding.vulnerability))
        .where(Finding.asset_id == asset.id)
        .order_by(Finding.risk_score.desc().nullslast(), Finding.id.asc())
    )
    findings = result.scalars().all()
    return [
        FindingDetailResponse(
            id=finding.id,
            asset_id=finding.asset_id,
            vuln_id=finding.vuln_id,
            status=finding.status,
            risk_score=finding.risk_score,
            first_seen=finding.first_seen,
            last_seen=finding.last_seen,
            vulnerability_title=finding.vulnerability.title,
            vulnerability_description=finding.vulnerability.description,
            vulnerability_severity=finding.vulnerability.severity,
            remediation=finding.vulnerability.remediation,
        )
        for finding in findings
    ]
