from __future__ import annotations

from typing import Any

from sqlalchemy.ext.asyncio import AsyncSession

from src.models.scan import AuditLog
from src.models.user import User


def add_audit_log(
    db: AsyncSession,
    *,
    user: User,
    action: str,
    entity_type: str,
    entity_id: int,
    payload: dict[str, Any] | None = None,
) -> AuditLog:
    audit = AuditLog(
        user_id=user.id,
        action=action,
        entity_type=entity_type,
        entity_id=entity_id,
        payload=payload,
    )
    db.add(audit)
    return audit
