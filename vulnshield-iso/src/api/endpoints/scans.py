from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from src.api.deps import get_db, require_roles
from src.models.asset import Asset
from src.models.credential import Credential
from src.models.scan import ScanTask
from src.models.user import User, UserRole
from src.schemas.scan import (
    DeviceTemplateDescriptor,
    ScanProfileDescriptor,
    ScanResponse,
    ScanTrigger,
)
from src.services.asset_inventory import ensure_scan_profile, ensure_template_key
from src.services.scan_catalog import credential_kind_supported_by_profile, list_device_templates, list_scan_profiles, profile_requires_credential
from src.services.scan_summary import normalize_scan_summary_payload
from src.worker.tasks import execute_scan


router = APIRouter(prefix='/scans', tags=['scans'])


def serialize_scan(task: ScanTask) -> ScanResponse:
    asset = task.asset
    credential = task.credential
    return ScanResponse(
        id=task.id,
        asset_id=task.asset_id,
        asset_name=asset.name if asset else None,
        asset_target=asset.target if asset else None,
        asset_device_type=asset.device_type if asset else None,
        scan_profile=ensure_scan_profile(task.scan_profile),
        device_template=ensure_template_key(task.device_template, asset.device_type if asset else None),
        credential_id=task.credential_id,
        credential_name=credential.name if credential else None,
        credential_kind=credential.kind if credential else None,
        status=task.status,
        started_at=task.started_at,
        finished_at=task.finished_at,
        raw_output_path=task.raw_output_path,
        scan_summary=normalize_scan_summary_payload(
            task.scan_summary,
            profile_key=ensure_scan_profile(task.scan_profile),
            device_template_key=ensure_template_key(task.device_template, asset.device_type if asset else None),
        ),
        scan_config=task.scan_config,
        error_message=task.error_message,
    )


@router.get('/profiles', response_model=list[ScanProfileDescriptor])
async def get_scan_profiles(
    _: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    return list_scan_profiles()


@router.get('/templates', response_model=list[DeviceTemplateDescriptor])
async def get_device_templates(
    _: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    return list_device_templates()


@router.get('', response_model=list[ScanResponse])
async def list_scans(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    statement = (
        select(ScanTask)
        .options(selectinload(ScanTask.asset), selectinload(ScanTask.credential))
        .order_by(ScanTask.id.desc())
    )
    if current_user.role == UserRole.ANALYST:
        statement = statement.join(Asset, Asset.id == ScanTask.asset_id).where(Asset.owner_id == current_user.id)

    result = await db.execute(statement)
    tasks = result.scalars().all()
    return [serialize_scan(task) for task in tasks]


@router.post('/trigger', response_model=ScanResponse, status_code=status.HTTP_202_ACCEPTED)
async def trigger_scan(
    trigger: ScanTrigger,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    asset = await db.get(Asset, trigger.asset_id)
    if asset is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到設備')

    credential = None
    if trigger.credential_id is not None:
        credential = await db.get(Credential, trigger.credential_id)
        if credential is None:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到 credential')
        if current_user.role == UserRole.ANALYST and credential.owner_id != current_user.id:
            raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法使用他人的 credential')

    normalized_profile = ensure_scan_profile(trigger.scan_profile)
    normalized_template = ensure_template_key(trigger.device_template, asset.device_type)
    if profile_requires_credential(normalized_profile):
        if credential is None:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='此掃描模式需要綁定 credential')
        if not credential_kind_supported_by_profile(normalized_profile, credential.kind):
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail='credential 類型與掃描模式不相容')

    if current_user.role == UserRole.ANALYST and asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法掃描他人的設備')

    new_task = ScanTask(
        asset_id=trigger.asset_id,
        scan_profile=normalized_profile,
        device_template=normalized_template,
        credential_id=credential.id if credential else None,
    )
    db.add(new_task)
    await db.commit()
    await db.refresh(new_task)

    execute_scan.delay(new_task.id)
    await db.refresh(new_task, attribute_names=['asset', 'credential'])
    return serialize_scan(new_task)


@router.get('/{task_id}/status', response_model=ScanResponse)
async def get_scan_status(
    task_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    task = await db.get(ScanTask, task_id)
    if task is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到掃描任務')

    asset = await db.get(Asset, task.asset_id)
    if current_user.role == UserRole.ANALYST and asset is not None and asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法查看他人的掃描任務')

    await db.refresh(task, attribute_names=['asset', 'credential'])
    return serialize_scan(task)
