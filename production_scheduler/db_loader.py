from __future__ import annotations

from datetime import datetime
from typing import Any

from .models import Machine, WorkOrder


def _qid(name: str) -> str:
    return f"[{name.replace(']', ']]')}]"


def _fetch_all_dict(cursor: Any, sql: str) -> list[dict[str, Any]]:
    cursor.execute(sql)
    columns = [col[0] for col in cursor.description]
    rows = cursor.fetchall()
    return [dict(zip(columns, row)) for row in rows]


def _to_int(value: Any, default: int = 0) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def load_from_database(
    conn: Any,
    machine_table: str = "機台資料表",
    fixed_table: str = "定品訂機表",
    work_table: str = "工作表",
    active_statuses: tuple[str, ...] = ("可用", "啟用", "正常", "1", "Y"),
    now: datetime | None = None,
) -> tuple[list[Machine], list[WorkOrder]]:
    now = now or datetime.now()
    cursor = conn.cursor()

    machines_sql = (
        f"SELECT {_qid('機台編號')} AS 機台編號, {_qid('機台製程代號')} AS 機台製程代號, "
        f"{_qid('機台狀態')} AS 機台狀態 FROM {_qid(machine_table)}"
    )
    fixed_sql = (
        f"SELECT {_qid('機台名稱')} AS 機台名稱, {_qid('產品圖號')} AS 產品圖號, {_qid('產品製程')} AS 產品製程 "
        f"FROM {_qid(fixed_table)}"
    )
    work_sql = (
        f"SELECT {_qid('製卡')} AS 製卡, {_qid('圖號')} AS 圖號, {_qid('製程順序')} AS 製程順序, "
        f"{_qid('製程代號')} AS 製程代號, {_qid('製程名稱')} AS 製程名稱, {_qid('預估工時')} AS 預估工時 "
        f"FROM {_qid(work_table)}"
    )

    machine_rows = _fetch_all_dict(cursor, machines_sql)
    fixed_rows = _fetch_all_dict(cursor, fixed_sql)
    work_rows = _fetch_all_dict(cursor, work_sql)

    allowed = {s.strip().upper() for s in active_statuses}
    machines: list[Machine] = []
    for row in machine_rows:
        status = str(row.get("機台狀態", "")).strip().upper()
        if allowed and status not in allowed:
            continue
        machines.append(
            Machine(
                id=str(row["機台編號"]).strip(),
                machine_type=str(row["機台製程代號"]).strip(),
                available_at=now,
            )
        )

    fixed_map: dict[tuple[str, str], set[str]] = {}
    for row in fixed_rows:
        part_no = str(row.get("產品圖號", "")).strip()
        process = str(row.get("產品製程", "")).strip()
        machine_name = str(row.get("機台名稱", "")).strip()
        if not part_no or not process or not machine_name:
            continue
        fixed_map.setdefault((part_no, process), set()).add(machine_name)

    work_rows.sort(key=lambda r: (str(r.get("製卡", "")), _to_int(r.get("製程順序", 0))))

    orders: list[WorkOrder] = []
    for row in work_rows:
        card = str(row.get("製卡", "")).strip()
        part_no = str(row.get("圖號", "")).strip()
        seq_raw = row.get("製程順序", 0)
        seq = _to_int(seq_raw, 0)
        process_code = str(row.get("製程代號", "")).strip()
        process_name = str(row.get("製程名稱", "")).strip()
        est_hours_raw = row.get("預估工時", 0)

        if not card or not process_code:
            continue

        try:
            est_hours = float(est_hours_raw)
        except (TypeError, ValueError):
            est_hours = 0.0

        if est_hours <= 0:
            continue

        preferred = fixed_map.get((part_no, process_code), set())
        if not preferred:
            preferred = fixed_map.get((part_no, process_name), set())

        orders.append(
            WorkOrder(
                id=f"{card}-{seq}-{process_code}",
                machine_type=process_code,
                process_hours=est_hours,
                preferred_machine_ids=tuple(sorted(preferred)),
                job_id=card,
                sequence=seq,
            )
        )

    return machines, orders
