from datetime import datetime, timezone
from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from src.api.deps import get_db, require_roles
from src.models.asset import Asset
from src.models.scan import AuditLog
from src.models.user import User, UserRole
from src.models.vulnerability import Finding
from src.schemas.vulnerability import FindingResponse, FindingStatusUpdate
from src.services.finding_lifecycle import validate_status_transition


router = APIRouter(prefix='/findings', tags=['findings'])


@router.get('', response_model=List[FindingResponse])
async def list_findings(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    statement = select(Finding).order_by(Finding.id.asc())
    if current_user.role == UserRole.ANALYST:
        statement = (
            select(Finding)
            .join(Asset, Asset.id == Finding.asset_id)
            .where(Asset.owner_id == current_user.id)
            .order_by(Finding.id.asc())
        )

    result = await db.execute(statement)
    return result.scalars().all()


@router.patch('/{finding_id}/status')
async def update_finding_status(
    finding_id: int,
    payload: FindingStatusUpdate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    finding = await db.get(Finding, finding_id)
    if finding is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到弱點紀錄')

    old_status = finding.status
    try:
        validate_status_transition(old_status, payload.new_status, current_user.role)
    except PermissionError as exc:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc

    finding.status = payload.new_status
    finding.last_seen = datetime.now(timezone.utc)

    audit = AuditLog(
        user_id=current_user.id,
        action='UPDATE_STATUS',
        entity_type='Finding',
        entity_id=finding_id,
        payload={'old': old_status.value, 'new': payload.new_status.value},
    )
    db.add(audit)
    await db.commit()
    return {'status': 'updated'}
