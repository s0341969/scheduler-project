from fastapi import APIRouter, Depends, HTTPException, Query, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from src.api.deps import get_db, require_roles
from src.models.asset import Asset
from src.models.credential import Credential
from src.models.scan import ScanSchedule
from src.models.user import User, UserRole
from src.schemas.schedule import ScanScheduleResponse, ScanScheduleUpdate
from src.services.audit_service import add_audit_log
from src.services.asset_inventory import ensure_asset_status, ensure_scan_profile, ensure_template_key
from src.services.scan_catalog import credential_kind_supported_by_profile, profile_requires_credential
from src.services.schedule_service import (
    calculate_next_run_at,
    normalize_schedule_payload_values,
    parse_weekdays,
    schedule_to_response,
    serialize_weekdays,
)


router = APIRouter(prefix='/schedules', tags=['schedules'])


async def get_accessible_schedule(schedule_id: int, db: AsyncSession, current_user: User) -> ScanSchedule:
    result = await db.execute(
        select(ScanSchedule)
        .options(selectinload(ScanSchedule.asset), selectinload(ScanSchedule.credential))
        .where(ScanSchedule.id == schedule_id)
    )
    schedule = result.scalar_one_or_none()
    if schedule is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到排程')
    if current_user.role == UserRole.ANALYST and schedule.asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法操作他人的排程')
    return schedule


async def resolve_schedule_credential(
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
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail='此 credential 已停用，無法綁定排程')
    return credential


@router.get('', response_model=list[ScanScheduleResponse])
async def list_schedules(
    asset_id: int | None = Query(default=None),
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    statement = (
        select(ScanSchedule)
        .options(selectinload(ScanSchedule.asset), selectinload(ScanSchedule.credential))
        .order_by(ScanSchedule.id.desc())
    )
    if asset_id is not None:
        statement = statement.where(ScanSchedule.asset_id == asset_id)
    if current_user.role == UserRole.ANALYST:
        statement = statement.join(Asset, Asset.id == ScanSchedule.asset_id).where(Asset.owner_id == current_user.id)

    schedules = (await db.execute(statement)).scalars().all()
    return [schedule_to_response(schedule) for schedule in schedules]


@router.patch('/{schedule_id}', response_model=ScanScheduleResponse)
async def update_schedule(
    schedule_id: int,
    payload: ScanScheduleUpdate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    schedule = await get_accessible_schedule(schedule_id, db, current_user)
    values = payload.model_dump(exclude_unset=True)
    original = {
        'name': schedule.name,
        'cadence': schedule.cadence,
        'timezone': schedule.timezone,
        'weekdays': schedule.weekdays,
        'run_hour': schedule.run_hour,
        'run_minute': schedule.run_minute,
        'cron_expr': schedule.cron_expr,
        'scan_profile': schedule.scan_profile,
        'device_template': schedule.device_template,
        'credential_id': schedule.credential_id,
        'is_active': schedule.is_active,
    }

    if 'credential_id' in values:
        fallback_credential_id = values['credential_id']
        if fallback_credential_id is None:
            fallback_credential_id = schedule.asset.default_credential_id
        credential = await resolve_schedule_credential(fallback_credential_id, db, current_user)
        values['credential_id'] = credential.id if credential else None
    else:
        credential = schedule.credential

    if 'scan_profile' in values and values['scan_profile'] is not None:
        values['scan_profile'] = ensure_scan_profile(values['scan_profile'])
    if 'device_template' in values:
        values['device_template'] = ensure_template_key(values['device_template'], schedule.asset.device_type)
    normalized_schedule = normalize_schedule_payload_values(
        cadence=values.get('cadence', schedule.cadence),
        timezone_name=values.get('timezone', schedule.timezone),
        weekdays=values.get('weekdays', parse_weekdays(schedule.weekdays)),
        run_hour=values.get('run_hour', schedule.run_hour),
        run_minute=values.get('run_minute', schedule.run_minute),
        cron_expr=values.get('cron_expr', schedule.cron_expr),
    )
    values['cadence'] = normalized_schedule['cadence']
    values['timezone'] = normalized_schedule['timezone']
    values['weekdays'] = serialize_weekdays(normalized_schedule['weekdays'])
    values['run_hour'] = normalized_schedule['run_hour']
    values['run_minute'] = normalized_schedule['run_minute']
    values['cron_expr'] = normalized_schedule['cron_expr']

    cadence = values['cadence']
    timezone_name = values['timezone']
    weekdays = normalized_schedule['weekdays']
    run_hour = values['run_hour']
    run_minute = values['run_minute']
    cron_expr = values['cron_expr']
    is_active = values.get('is_active', schedule.is_active)
    scan_profile = values.get('scan_profile', schedule.scan_profile)

    if ensure_asset_status(schedule.asset.status) == 'Retired' and is_active:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail='退役設備不可啟用排程')
    if profile_requires_credential(scan_profile):
        if credential is None:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='此排程模式需要綁定 credential')
        if not credential_kind_supported_by_profile(scan_profile, credential.kind):
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='credential 類型與排程掃描模式不相容')

    for field, value in values.items():
        setattr(schedule, field, value)

    schedule.next_run_at = (
        calculate_next_run_at(
            cadence=cadence,
            timezone_name=timezone_name,
            run_hour=run_hour,
            run_minute=run_minute,
            weekdays=weekdays,
            cron_expr=cron_expr,
        )
        if is_active
        else None
    )
    if not is_active:
        schedule.last_error = None

    changes = {}
    for field, old_value in original.items():
        new_value = getattr(schedule, field)
        if old_value != new_value:
            changes[field] = {'old': old_value, 'new': new_value}
    if changes:
        add_audit_log(
            db,
            user=current_user,
            action='UPDATE_SCHEDULE',
            entity_type='ScanSchedule',
            entity_id=schedule.id,
            payload=changes,
        )

    await db.commit()
    await db.refresh(schedule, attribute_names=['asset', 'credential'])
    return schedule_to_response(schedule)


@router.delete('/{schedule_id}', status_code=status.HTTP_204_NO_CONTENT)
async def delete_schedule(
    schedule_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    schedule = await get_accessible_schedule(schedule_id, db, current_user)
    add_audit_log(
        db,
        user=current_user,
        action='DELETE_SCHEDULE',
        entity_type='ScanSchedule',
        entity_id=schedule.id,
        payload={'asset_id': schedule.asset_id, 'name': schedule.name},
    )
    await db.delete(schedule)
    await db.commit()
