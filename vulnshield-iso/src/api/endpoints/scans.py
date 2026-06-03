from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession

from src.api.deps import get_db, require_roles
from src.models.asset import Asset
from src.models.scan import ScanTask
from src.models.user import User, UserRole
from src.schemas.scan import ScanResponse, ScanTrigger
from src.worker.tasks import execute_scan


router = APIRouter(prefix='/scans', tags=['scans'])


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
    return new_task


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

    return task
