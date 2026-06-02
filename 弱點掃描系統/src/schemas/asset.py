from typing import Optional
from pydantic import BaseModel, Field
from datetime import datetime
from src.models.asset import EnvType

class AssetBase(BaseModel):
    name: str = Field(..., example='Main API Server')
    target: str = Field(..., example='192.168.1.10')
    criticality: int = Field(..., ge=1, le=5, example=5)
    env: EnvType
    status: str = 'Active'

class AssetCreate(AssetBase):
    owner_id: int

class AssetUpdate(BaseModel):
    name: Optional[str] = None
    target: Optional[str] = None
    criticality: Optional[int] = Field(None, ge=1, le=5)
    env: Optional[EnvType] = None
    status: Optional[str] = None

class AssetResponse(AssetBase):
    id: int
    owner_id: int
    created_at: datetime

    class Config:
        from_attributes = True
