from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from src.api.deps import get_db, require_roles
from src.models.asset import Asset
from src.models.scan import ScanTask
from src.models.user import User, UserRole
from src.schemas.scan import ScanResponse, ScanTrigger
from src.worker.tasks import execute_scan


router = APIRouter(prefix='/scans', tags=['scans'])


def serialize_scan(task: ScanTask) -> ScanResponse:
    asset = task.asset
    return ScanResponse(
        id=task.id,
        asset_id=task.asset_id,
        asset_name=asset.name if asset else None,
        asset_target=asset.target if asset else None,
        asset_device_type=asset.device_type if asset else None,
        scan_profile=task.scan_profile,
        status=task.status,
        started_at=task.started_at,
        finished_at=task.finished_at,
        raw_output_path=task.raw_output_path,
        scan_summary=task.scan_summary,
        error_message=task.error_message,
    )


@router.get('', response_model=list[ScanResponse])
async def list_scans(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    statement = (
        select(ScanTask)
        .options(selectinload(ScanTask.asset))
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

    if current_user.role == UserRole.ANALYST and asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法掃描他人的設備')

    new_task = ScanTask(asset_id=trigger.asset_id, scan_profile=trigger.scan_profile)
    db.add(new_task)
    await db.commit()
    await db.refresh(new_task)

    execute_scan.delay(new_task.id)
    await db.refresh(new_task, attribute_names=['asset'])
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

    await db.refresh(task, attribute_names=['asset'])
    return serialize_scan(task)
