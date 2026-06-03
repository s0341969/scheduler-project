from pydantic import BaseModel
from typing import Optional
from datetime import datetime
from src.models.asset import DeviceType
from src.models.scan import ScanStatus


class ScanEngineSummary(BaseModel):
    name: str
    status: str
    detail: Optional[str] = None


class DiscoveredService(BaseModel):
    port: str
    protocol: str
    state: str
    service: str
    product: Optional[str] = None


class ScanSignal(BaseModel):
    title: str
    severity: str
    template_id: Optional[str] = None
    matcher_name: Optional[str] = None
    matched_at: Optional[str] = None
    description: Optional[str] = None


class ScanSummaryResponse(BaseModel):
    engines: list[ScanEngineSummary]
    services: list[DiscoveredService]
    vulnerabilities: list[ScanSignal]
    informational: list[ScanSignal]
    service_count: int
    vulnerability_count: int
    informational_count: int


class ScanTrigger(BaseModel):
    asset_id: int
    scan_profile: str = 'full'

class ScanResponse(BaseModel):
    id: int
    asset_id: int
    asset_name: Optional[str] = None
    asset_target: Optional[str] = None
    asset_device_type: Optional[DeviceType] = None
    scan_profile: str
    status: ScanStatus
    started_at: Optional[datetime] = None
    finished_at: Optional[datetime] = None
    raw_output_path: Optional[str] = None
    scan_summary: Optional[ScanSummaryResponse] = None
    error_message: Optional[str] = None

    class Config:
        from_attributes = True
