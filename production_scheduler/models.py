from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timedelta


@dataclass(frozen=True)
class WorkOrder:
    id: str
    machine_type: str
    process_hours: float
    priority: int = 3
    due_at: datetime | None = None
    release_at: datetime | None = None
    preferred_machine_ids: tuple[str, ...] = ()
    job_id: str = ""
    sequence: int = 0


@dataclass(frozen=True)
class Machine:
    id: str
    machine_type: str
    available_at: datetime


@dataclass(frozen=True)
class ScheduleItem:
    order_id: str
    machine_id: str
    start_at: datetime
    end_at: datetime
    due_at: datetime | None
    job_id: str = ""
    sequence: int = 0

    @property
    def duration(self) -> timedelta:
        return self.end_at - self.start_at

    @property
    def tardiness_hours(self) -> float:
        if self.due_at is None:
            return 0.0
        delay = self.end_at - self.due_at
        return max(0.0, delay.total_seconds() / 3600.0)
