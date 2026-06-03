from typing import List

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from src.api.deps import get_current_user, get_db, require_roles
from src.models.asset import Asset
from src.models.user import User, UserRole
from src.schemas.asset import AssetCreate, AssetResponse, AssetUpdate


router = APIRouter(prefix='/assets', tags=['assets'])


@router.post('', response_model=AssetResponse, status_code=status.HTTP_201_CREATED)
async def create_asset(
    asset: AssetCreate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    if current_user.role == UserRole.ANALYST and current_user.id != asset.owner_id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='Analyst 只能建立自己的資產')

    new_asset = Asset(**asset.model_dump())
    db.add(new_asset)
    await db.commit()
    await db.refresh(new_asset)
    return new_asset


@router.get('', response_model=List[AssetResponse])
async def list_assets(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user),
):
    statement = select(Asset)
    if current_user.role == UserRole.ANALYST:
        statement = statement.where(Asset.owner_id == current_user.id)

    result = await db.execute(statement.order_by(Asset.id.asc()))
    return result.scalars().all()


@router.patch('/{asset_id}', response_model=AssetResponse)
async def update_asset(
    asset_id: int,
    payload: AssetUpdate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    asset = await db.get(Asset, asset_id)
    if asset is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到資產')

    if current_user.role == UserRole.ANALYST and asset.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法修改他人的資產')

    for field, value in payload.model_dump(exclude_unset=True).items():
        setattr(asset, field, value)

    await db.commit()
    await db.refresh(asset)
    return asset
