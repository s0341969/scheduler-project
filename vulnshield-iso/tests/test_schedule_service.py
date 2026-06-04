import unittest
from datetime import datetime, timezone

from src.services.schedule_service import (
    calculate_next_run_at,
    ensure_schedule_cadence,
    normalize_weekdays,
    validate_cron_expr,
)


class ScheduleServiceTests(unittest.TestCase):
    def test_ensure_schedule_cadence_defaults_to_daily(self):
        self.assertEqual(ensure_schedule_cadence('unknown'), 'Daily')

    def test_normalize_weekdays_sorts_and_deduplicates(self):
        self.assertEqual(normalize_weekdays([4, 0, 4, 2]), [0, 2, 4])

    def test_validate_cron_expr_accepts_simple_weekday_expression(self):
        self.assertEqual(validate_cron_expr('0 2 * * 1-5'), '0 2 * * 1-5')

    def test_calculate_next_run_at_for_daily_schedule(self):
        next_run = calculate_next_run_at(
            cadence='Daily',
            timezone_name='Asia/Taipei',
            run_hour=2,
            run_minute=0,
            weekdays=[],
            cron_expr=None,
            base_time=datetime(2026, 6, 4, 0, 30, tzinfo=timezone.utc),
        )
        self.assertIsNotNone(next_run)
        self.assertGreater(next_run, datetime(2026, 6, 4, 0, 30, tzinfo=timezone.utc))

    def test_calculate_next_run_at_for_weekly_schedule(self):
        next_run = calculate_next_run_at(
            cadence='Weekly',
            timezone_name='Asia/Taipei',
            run_hour=3,
            run_minute=15,
            weekdays=[0, 2],
            cron_expr=None,
            base_time=datetime(2026, 6, 4, 0, 30, tzinfo=timezone.utc),
        )
        self.assertIsNotNone(next_run)

    def test_calculate_next_run_at_for_cron_schedule(self):
        next_run = calculate_next_run_at(
            cadence='Cron',
            timezone_name='Asia/Taipei',
            run_hour=0,
            run_minute=0,
            weekdays=[],
            cron_expr='0 2 * * *',
            base_time=datetime(2026, 6, 4, 0, 30, tzinfo=timezone.utc),
        )
        self.assertIsNotNone(next_run)

