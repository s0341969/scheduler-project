from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field, ConfigDict

from src.models.asset import DeviceType, EnvType
from src.models.scan import ScanStatus


class AssetBase(BaseModel):
    name: str = Field(..., example='Main API Server')
    target: str = Field(..., example='192.168.1.10')
    criticality: int = Field(..., ge=1, le=5, example=5)
    env: EnvType
    device_type: DeviceType = DeviceType.COMPUTER
    location: Optional[str] = Field(default=None, example='台北總部機房')
    tags: list[str] = Field(default_factory=list, example=['總部', '外網設備'])
    notes: Optional[str] = Field(default=None, example='需於維護時段執行高強度掃描')
    status: str = 'Active'


class AssetCreate(AssetBase):
    owner_id: int


class AssetUpdate(BaseModel):
    name: Optional[str] = None
    target: Optional[str] = None
    criticality: Optional[int] = Field(None, ge=1, le=5)
    env: Optional[EnvType] = None
    device_type: Optional[DeviceType] = None
    location: Optional[str] = None
    tags: Optional[list[str]] = None
    notes: Optional[str] = None
    status: Optional[str] = None


class AssetResponse(AssetBase):
    id: int
    owner_id: int
    created_at: datetime
    updated_at: Optional[datetime] = None
    total_findings: int = 0
    open_findings: int = 0
    high_risk_findings: int = 0
    last_scan_id: Optional[int] = None
    last_scan_status: Optional[ScanStatus] = None
    last_scan_at: Optional[datetime] = None

    model_config = ConfigDict(from_attributes=True)
