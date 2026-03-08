from __future__ import annotations

import argparse
import csv
import os
from datetime import datetime

import pyodbc

from production_scheduler import build_schedule, load_from_database


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build production schedule from MSSQL tables")
    parser.add_argument("--conn", default=os.getenv("PROD_SCHEDULER_CONN", ""), help="MSSQL ODBC connection string")
    parser.add_argument("--machine-table", default="機台資料表")
    parser.add_argument("--fixed-table", default="定品訂機表")
    parser.add_argument("--work-table", default="工作表")
    parser.add_argument("--output", default="schedule_output.csv")
    parser.add_argument("--as-of", default="", help="Machine availability baseline, format: YYYY-MM-DD HH:MM:SS")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if not args.conn:
        raise SystemExit("Missing DB connection string. Use --conn or PROD_SCHEDULER_CONN.")

    now = datetime.now()
    if args.as_of:
        now = datetime.strptime(args.as_of, "%Y-%m-%d %H:%M:%S")

    conn = pyodbc.connect(args.conn)
    try:
        machines, orders = load_from_database(
            conn=conn,
            machine_table=args.machine_table,
            fixed_table=args.fixed_table,
            work_table=args.work_table,
            now=now,
        )
    finally:
        conn.close()

    if not machines:
        raise SystemExit("No available machine records found.")
    if not orders:
        raise SystemExit("No schedulable work records found.")

    schedule = build_schedule(orders=orders, machines=machines)

    with open(args.output, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(["job_id", "sequence", "order_id", "machine_id", "start_at", "end_at", "tardiness_hours"])
        for item in schedule:
            writer.writerow(
                [
                    item.job_id,
                    item.sequence,
                    item.order_id,
                    item.machine_id,
                    item.start_at.strftime("%Y-%m-%d %H:%M:%S"),
                    item.end_at.strftime("%Y-%m-%d %H:%M:%S"),
                    f"{item.tardiness_hours:.2f}",
                ]
            )

    print(f"Machines: {len(machines)}")
    print(f"Operations: {len(orders)}")
    print(f"Schedule rows: {len(schedule)}")
    print(f"Output: {args.output}")


if __name__ == "__main__":
    main()
