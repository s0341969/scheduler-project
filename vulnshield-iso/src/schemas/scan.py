from pydantic import BaseModel
from typing import Optional
from datetime import datetime
from src.models.asset import DeviceType
from src.models.scan import ScanStatus


class ScanEngineSummary(BaseModel):
    name: str
    status: str
    detail: Optional[str] = None
    scope: Optional[str] = None


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
    profile_key: str
    profile_label: str
    profile_scope: str
    device_template_key: str
    device_template_label: str
    authentication: dict
    engines: list[ScanEngineSummary]
    services: list[DiscoveredService]
    vulnerabilities: list[ScanSignal]
    misconfigurations: list[ScanSignal]
    certificate_risks: list[ScanSignal]
    exposures: list[ScanSignal]
    informational: list[ScanSignal]
    service_count: int
    vulnerability_count: int
    misconfiguration_count: int
    certificate_risk_count: int
    exposure_count: int
    informational_count: int
    actionable_signal_count: int


class ScanTrigger(BaseModel):
    asset_id: int
    scan_profile: str = 'standard'
    device_template: Optional[str] = None
    credential_id: Optional[int] = None


class AssetScanRequest(BaseModel):
    scan_profile: Optional[str] = None
    device_template: Optional[str] = None
    credential_id: Optional[int] = None


class ScanProfileDescriptor(BaseModel):
    key: str
    label: str
    description: str
    scope: str
    intensity: str
    recommended_for: list[str]
    run_nmap: bool
    run_nuclei: bool
    nmap_args: list[str]
    nuclei_tags: list[str]
    max_duration_seconds: int
    authenticated: bool = False
    required_credential_kinds: list[str] = []


class DeviceTemplateDescriptor(BaseModel):
    key: str
    label: str
    description: str
    recommended_profile: str
    device_types: list[str]
    nuclei_tags: list[str]
    management_ports: list[int]

class ScanResponse(BaseModel):
    id: int
    asset_id: int
    asset_name: Optional[str] = None
    asset_target: Optional[str] = None
    asset_device_type: Optional[DeviceType] = None
    schedule_id: Optional[int] = None
    scan_profile: str
    device_template: Optional[str] = None
    credential_id: Optional[int] = None
    credential_name: Optional[str] = None
    credential_kind: Optional[str] = None
    status: ScanStatus
    started_at: Optional[datetime] = None
    finished_at: Optional[datetime] = None
    raw_output_path: Optional[str] = None
    scan_summary: Optional[ScanSummaryResponse] = None
    scan_config: Optional[dict] = None
    error_message: Optional[str] = None

    class Config:
        from_attributes = True
