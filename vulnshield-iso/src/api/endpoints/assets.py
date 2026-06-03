from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from src.api.deps import get_current_user, get_db, require_roles
from src.models.asset import Asset
from src.models.scan import ScanTask
from src.models.user import User, UserRole
from src.models.vulnerability import Finding
from src.schemas.asset import AssetCreate, AssetResponse, AssetUpdate
from src.schemas.scan import ScanResponse
from src.schemas.vulnerability import FindingDetailResponse
from src.services.asset_inventory import asset_to_response, build_asset_summary_map, normalize_tags
from src.worker.tasks import execute_scan


router = APIRouter(prefix='/assets', tags=['assets'])


async def get_accessible_asset(asset_id: int, db: AsyncSession, current_user: User) -> Asset:
    asset = await db.get(Asset, asset_id)
    if asset is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到設備')

    if current_user.role == UserRole.ANALYST and asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法操作他人的設備')

    return asset


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
    payload['tags'] = normalize_tags(payload['tags'])

    new_asset = Asset(**payload)
    db.add(new_asset)
    await db.commit()
    await db.refresh(new_asset)

    summary_map = await build_asset_summary_map(db, [new_asset])
    return asset_to_response(new_asset, summary_map.get(new_asset.id))


@router.get('', response_model=List[AssetResponse])
async def list_assets(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user),
):
    statement = select(Asset)
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

    values = payload.model_dump(exclude_unset=True)
    if 'device_type' in values and values['device_type'] is not None:
        values['device_type'] = values['device_type'].value
    if 'tags' in values:
        values['tags'] = normalize_tags(values['tags'])

    for field, value in values.items():
        setattr(asset, field, value)

    await db.commit()
    await db.refresh(asset)
    summary_map = await build_asset_summary_map(db, [asset])
    return asset_to_response(asset, summary_map.get(asset.id))


@router.post('/{asset_id}/scan', response_model=ScanResponse, status_code=status.HTTP_202_ACCEPTED)
async def trigger_asset_scan(
    asset_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    new_task = ScanTask(asset_id=asset.id, scan_profile='full')
    db.add(new_task)
    await db.commit()
    await db.refresh(new_task)

    execute_scan.delay(new_task.id)
    return new_task


@router.get('/{asset_id}/scans', response_model=List[ScanResponse])
async def list_asset_scans(
    asset_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    asset = await get_accessible_asset(asset_id, db, current_user)
    result = await db.execute(
        select(ScanTask)
        .where(ScanTask.asset_id == asset.id)
        .order_by(ScanTask.id.desc())
    )
    return result.scalars().all()


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
