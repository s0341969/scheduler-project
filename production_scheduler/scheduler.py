from __future__ import annotations

from datetime import datetime, timedelta
from typing import Iterable

from .models import Machine, ScheduleItem, WorkOrder


def _order_sort_key(order: WorkOrder) -> tuple:
    due_rank = order.due_at or datetime.max
    release_rank = order.release_at or datetime.min
    # priority: smaller number means higher priority
    return (order.priority, due_rank, release_rank, order.id)


def _choose_machine(candidates: list[dict[str, object]], preferred_ids: tuple[str, ...]) -> dict[str, object]:
    if preferred_ids:
        preferred_set = set(preferred_ids)
        preferred = [m for m in candidates if str(m["id"]) in preferred_set]
        if preferred:
            candidates = preferred
    return min(candidates, key=lambda m: (m["available_at"], m["id"]))


def build_schedule(
    orders: Iterable[WorkOrder],
    machines: Iterable[Machine],
) -> list[ScheduleItem]:
    """Build forward finite-capacity schedule with process precedence.

    Precedence rule:
    - Within the same job_id, larger sequence starts after predecessor ends.
    """
    machine_pool: dict[str, list[dict[str, object]]] = {}
    for machine in machines:
        machine_pool.setdefault(machine.machine_type, []).append(
            {"id": machine.id, "available_at": machine.available_at}
        )

    grouped: dict[str, list[WorkOrder]] = {}
    standalone_counter = 0
    for o in orders:
        if o.job_id:
            key = o.job_id
        else:
            standalone_counter += 1
            key = f"__single_{standalone_counter}_{o.id}"
        grouped.setdefault(key, []).append(o)

    for key in grouped:
        grouped[key].sort(key=lambda x: (x.sequence, _order_sort_key(x)))

    next_idx: dict[str, int] = {k: 0 for k in grouped}
    ready: list[tuple[str, WorkOrder]] = [(k, ops[0]) for k, ops in grouped.items() if ops]

    schedule: list[ScheduleItem] = []
    total = sum(len(v) for v in grouped.values())
    done = 0

    while done < total:
        if not ready:
            raise ValueError("No schedulable operation found. Check process route data.")

        job_key, current = min(ready, key=lambda x: _order_sort_key(x[1]))
        ready.remove((job_key, current))

        candidates = machine_pool.get(current.machine_type, [])
        if not candidates:
            raise ValueError(f"No machine found for machine_type={current.machine_type!r}")

        chosen = _choose_machine(candidates, current.preferred_machine_ids)
        chosen_available = chosen["available_at"]
        assert isinstance(chosen_available, datetime)

        release_at = current.release_at or datetime.min
        start_at = max(chosen_available, release_at)
        end_at = start_at + timedelta(hours=current.process_hours)

        schedule.append(
            ScheduleItem(
                order_id=current.id,
                machine_id=str(chosen["id"]),
                start_at=start_at,
                end_at=end_at,
                due_at=current.due_at,
                job_id=current.job_id,
                sequence=current.sequence,
            )
        )
        chosen["available_at"] = end_at
        done += 1

        idx = next_idx[job_key] + 1
        next_idx[job_key] = idx
        if idx < len(grouped[job_key]):
            nxt = grouped[job_key][idx]
            inherited_release = end_at
            explicit_release = nxt.release_at
            if explicit_release is None or explicit_release < inherited_release:
                nxt = WorkOrder(
                    id=nxt.id,
                    machine_type=nxt.machine_type,
                    process_hours=nxt.process_hours,
                    priority=nxt.priority,
                    due_at=nxt.due_at,
                    release_at=inherited_release,
                    preferred_machine_ids=nxt.preferred_machine_ids,
                    job_id=nxt.job_id,
                    sequence=nxt.sequence,
                )
                grouped[job_key][idx] = nxt
            ready.append((job_key, nxt))

    return schedule
