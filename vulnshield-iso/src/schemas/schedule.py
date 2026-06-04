from datetime import datetime
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class ScanScheduleBase(BaseModel):
    name: str = Field(..., example='總部核心交換器每日弱掃')
    cadence: str = Field(default='Daily', example='Weekly')
    timezone: str = Field(default='Asia/Taipei', example='Asia/Taipei')
    weekdays: list[int] = Field(default_factory=list, example=[0, 2, 4])
    run_hour: Optional[int] = Field(default=2, ge=0, le=23)
    run_minute: Optional[int] = Field(default=0, ge=0, le=59)
    cron_expr: Optional[str] = Field(default=None, example='0 2 * * 1-5')
    scan_profile: str = Field(default='standard', example='authenticated_snmp')
    device_template: Optional[str] = Field(default=None, example='switch')
    credential_id: Optional[int] = Field(default=None, example=2)
    is_active: bool = True


class ScanScheduleCreate(ScanScheduleBase):
    pass


class ScanScheduleUpdate(BaseModel):
    name: Optional[str] = None
    cadence: Optional[str] = None
    timezone: Optional[str] = None
    weekdays: Optional[list[int]] = None
    run_hour: Optional[int] = Field(default=None, ge=0, le=23)
    run_minute: Optional[int] = Field(default=None, ge=0, le=59)
    cron_expr: Optional[str] = None
    scan_profile: Optional[str] = None
    device_template: Optional[str] = None
    credential_id: Optional[int] = None
    is_active: Optional[bool] = None


class ScanScheduleResponse(ScanScheduleBase):
    id: int
    asset_id: int
    asset_name: Optional[str] = None
    credential_name: Optional[str] = None
    cadence_label: str
    weekdays_label: Optional[str] = None
    next_run_at: Optional[datetime] = None
    last_run_at: Optional[datetime] = None
    last_task_id: Optional[int] = None
    last_error: Optional[str] = None
    created_at: datetime
    updated_at: Optional[datetime] = None

    model_config = ConfigDict(from_attributes=True)
