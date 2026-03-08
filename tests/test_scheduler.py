from datetime import datetime
import unittest

from production_scheduler import Machine, WorkOrder, build_schedule


class SchedulerTests(unittest.TestCase):
    def test_assigns_to_earliest_machine(self) -> None:
        t0 = datetime(2026, 3, 6, 8, 0)
        machines = [
            Machine(id="M1", machine_type="A", available_at=t0),
            Machine(id="M2", machine_type="A", available_at=t0),
        ]
        orders = [
            WorkOrder(id="O1", machine_type="A", process_hours=2, priority=1),
            WorkOrder(id="O2", machine_type="A", process_hours=2, priority=1),
            WorkOrder(id="O3", machine_type="A", process_hours=1, priority=1),
        ]

        result = build_schedule(orders, machines)

        self.assertEqual(result[0].machine_id, "M1")
        self.assertEqual(result[1].machine_id, "M2")
        self.assertEqual(result[2].start_at, datetime(2026, 3, 6, 10, 0))

    def test_respects_priority_and_due_date(self) -> None:
        t0 = datetime(2026, 3, 6, 8, 0)
        machines = [Machine(id="M1", machine_type="A", available_at=t0)]
        orders = [
            WorkOrder(id="O-late-prio", machine_type="A", process_hours=1, priority=2, due_at=datetime(2026, 3, 6, 10, 0)),
            WorkOrder(id="O-high-prio", machine_type="A", process_hours=1, priority=1, due_at=datetime(2026, 3, 6, 12, 0)),
            WorkOrder(id="O-high-prio-early-due", machine_type="A", process_hours=1, priority=1, due_at=datetime(2026, 3, 6, 9, 0)),
        ]

        result = build_schedule(orders, machines)

        self.assertEqual([r.order_id for r in result], ["O-high-prio-early-due", "O-high-prio", "O-late-prio"])

    def test_prefers_fixed_machine_if_provided(self) -> None:
        t0 = datetime(2026, 3, 6, 8, 0)
        machines = [
            Machine(id="M1", machine_type="A", available_at=t0),
            Machine(id="M2", machine_type="A", available_at=t0),
        ]
        orders = [
            WorkOrder(id="O1", machine_type="A", process_hours=1, preferred_machine_ids=("M2",)),
        ]

        result = build_schedule(orders, machines)
        self.assertEqual(result[0].machine_id, "M2")

    def test_process_precedence_by_job(self) -> None:
        t0 = datetime(2026, 3, 6, 8, 0)
        machines = [
            Machine(id="CUT-01", machine_type="CUT", available_at=t0),
            Machine(id="DRILL-01", machine_type="DRILL", available_at=t0),
        ]
        orders = [
            WorkOrder(id="CARD1-20", job_id="CARD1", sequence=20, machine_type="DRILL", process_hours=1),
            WorkOrder(id="CARD1-10", job_id="CARD1", sequence=10, machine_type="CUT", process_hours=2),
        ]

        result = build_schedule(orders, machines)
        by_id = {x.order_id: x for x in result}

        self.assertGreaterEqual(by_id["CARD1-20"].start_at, by_id["CARD1-10"].end_at)

    def test_no_machine_raises(self) -> None:
        with self.assertRaises(ValueError):
            build_schedule(
                orders=[WorkOrder(id="O1", machine_type="X", process_hours=1)],
                machines=[Machine(id="M1", machine_type="A", available_at=datetime(2026, 3, 6, 8, 0))],
            )


if __name__ == "__main__":
    unittest.main()
