from datetime import datetime

from production_scheduler import Machine, WorkOrder, build_schedule


def main() -> None:
    t0 = datetime(2026, 3, 6, 8, 0, 0)

    machines = [
        Machine(id="MILL-01", machine_type="MILL", available_at=t0),
        Machine(id="MILL-02", machine_type="MILL", available_at=t0),
        Machine(id="LATHE-01", machine_type="LATHE", available_at=t0),
    ]

    orders = [
        WorkOrder(id="WO-1001", machine_type="MILL", process_hours=2, priority=1, due_at=datetime(2026, 3, 6, 12, 0)),
        WorkOrder(id="WO-1002", machine_type="MILL", process_hours=4, priority=2, due_at=datetime(2026, 3, 6, 16, 0)),
        WorkOrder(id="WO-1003", machine_type="LATHE", process_hours=3, priority=1, due_at=datetime(2026, 3, 6, 15, 0)),
        WorkOrder(id="WO-1004", machine_type="MILL", process_hours=1.5, priority=1, due_at=datetime(2026, 3, 6, 11, 30)),
    ]

    result = build_schedule(orders=orders, machines=machines)

    print("order_id,machine_id,start_at,end_at,tardiness_hours")
    for row in result:
        print(
            f"{row.order_id},{row.machine_id},{row.start_at.isoformat(sep=' ')},{row.end_at.isoformat(sep=' ')},{row.tardiness_hours:.2f}"
        )


if __name__ == "__main__":
    main()