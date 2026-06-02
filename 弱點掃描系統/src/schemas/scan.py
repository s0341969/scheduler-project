from pydantic import BaseModel
from typing import Optional
from datetime import datetime
from src.models.scan import ScanStatus

class ScanTrigger(BaseModel):
    asset_id: int
    scan_profile: str = 'full'

class ScanResponse(BaseModel):
    id: int
    asset_id: int
    status: ScanStatus
    started_at: Optional[datetime] = None
    finished_at: Optional[datetime] = None

    class Config:
        from_attributes = True
