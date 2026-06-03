from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict


class AuditLogResponse(BaseModel):
    id: int
    user_id: int
    action: str
    entity_type: str
    entity_id: int
    payload: dict[str, Any] | None = None
    timestamp: datetime

    model_config = ConfigDict(from_attributes=True)
