from __future__ import annotations

from datetime import datetime, timedelta, timezone
from typing import Iterable
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from src.models.scan import ScanSchedule, ScheduleCadence
from src.services.asset_inventory import ensure_scan_profile, ensure_template_key

WEEKDAY_LABELS = {
    0: '週一',
    1: '週二',
    2: '週三',
    3: '週四',
    4: '週五',
    5: '週六',
    6: '週日',
}

CADENCE_LABELS = {
    ScheduleCadence.DAILY.value: '每日',
    ScheduleCadence.WEEKLY.value: '每週',
    ScheduleCadence.CRON.value: 'Cron',
}


def ensure_schedule_cadence(raw_value: str | None) -> str:
    if raw_value in {cadence.value for cadence in ScheduleCadence}:
        return raw_value
    return ScheduleCadence.DAILY.value


def ensure_schedule_timezone(raw_timezone: str | None) -> str:
    timezone_name = (raw_timezone or 'Asia/Taipei').strip() or 'Asia/Taipei'
    try:
        ZoneInfo(timezone_name)
    except ZoneInfoNotFoundError as exc:
        raise ValueError('不支援的時區名稱') from exc
    return timezone_name


def normalize_weekdays(values: Iterable[int] | None) -> list[int]:
    if values is None:
        return []
    normalized = sorted({int(value) for value in values if 0 <= int(value) <= 6})
    return normalized


def serialize_weekdays(values: Iterable[int] | None) -> str | None:
    normalized = normalize_weekdays(values)
    return ','.join(str(value) for value in normalized) if normalized else None


def parse_weekdays(raw_value: str | None) -> list[int]:
    if not raw_value:
        return []
    return normalize_weekdays(int(part) for part in raw_value.split(',') if part.strip())


def weekdays_label(values: Iterable[int] | None) -> str | None:
    normalized = normalize_weekdays(values)
    if not normalized:
        return None
    return '、'.join(WEEKDAY_LABELS.get(value, str(value)) for value in normalized)


def _parse_field_token(token: str, min_value: int, max_value: int) -> set[int]:
    token = token.strip()
    if token == '*':
        return set(range(min_value, max_value + 1))
    if token.startswith('*/'):
        step = int(token[2:])
        if step <= 0:
            raise ValueError('Cron step 必須大於 0')
        return set(range(min_value, max_value + 1, step))
    if '-' in token:
        range_token, _, step_token = token.partition('/')
        start_text, end_text = range_token.split('-', 1)
        start = int(start_text)
        end = int(end_text)
        if start > end:
            raise ValueError('Cron 範圍起始值不可大於結束值')
        step = int(step_token) if step_token else 1
        return {value for value in range(start, end + 1, step) if min_value <= value <= max_value}
    if '/' in token:
        base_text, step_text = token.split('/', 1)
        base = int(base_text)
        step = int(step_text)
        return {value for value in range(base, max_value + 1, step) if min_value <= value <= max_value}
    value = int(token)
    if token == '7' and min_value == 0 and max_value == 6:
        value = 0
    if not (min_value <= value <= max_value):
        raise ValueError('Cron 欄位超出允許範圍')
    return {value}


def _parse_cron_field(field: str, min_value: int, max_value: int) -> set[int]:
    values: set[int] = set()
    for token in field.split(','):
        values.update(_parse_field_token(token, min_value, max_value))
    if not values:
        raise ValueError('Cron 欄位不可為空')
    return values


def validate_cron_expr(cron_expr: str | None) -> str:
    normalized = (cron_expr or '').strip()
    if not normalized:
        raise ValueError('Cron 排程需要 cron_expr')
    parts = normalized.split()
    if len(parts) != 5:
        raise ValueError('Cron 排程格式必須為 5 欄位')
    _parse_cron_field(parts[0], 0, 59)
    _parse_cron_field(parts[1], 0, 23)
    _parse_cron_field(parts[2], 1, 31)
    _parse_cron_field(parts[3], 1, 12)
    _parse_cron_field(parts[4], 0, 6)
    return normalized


def validate_schedule_payload(
    *,
    cadence: str,
    timezone_name: str,
    run_hour: int | None,
    run_minute: int | None,
    weekdays: Iterable[int] | None,
    cron_expr: str | None,
) -> None:
    _ = ensure_schedule_timezone(timezone_name)
    normalized_cadence = ensure_schedule_cadence(cadence)
    normalized_weekdays = normalize_weekdays(weekdays)

    if normalized_cadence in {ScheduleCadence.DAILY.value, ScheduleCadence.WEEKLY.value}:
        if run_hour is None or run_minute is None:
            raise ValueError('每日 / 每週排程需要指定執行時間')
    if normalized_cadence == ScheduleCadence.WEEKLY.value and not normalized_weekdays:
        raise ValueError('每週排程至少需要選擇一個星期')
    if normalized_cadence == ScheduleCadence.CRON.value:
        validate_cron_expr(cron_expr)


def normalize_schedule_payload_values(
    *,
    cadence: str | None,
    timezone_name: str | None,
    weekdays: Iterable[int] | None,
    run_hour: int | None,
    run_minute: int | None,
    cron_expr: str | None,
) -> dict:
    normalized_cadence = ensure_schedule_cadence(cadence)
    normalized_timezone = ensure_schedule_timezone(timezone_name)
    normalized_weekdays = normalize_weekdays(weekdays)
    normalized_cron = cron_expr.strip() if cron_expr else None
    validate_schedule_payload(
        cadence=normalized_cadence,
        timezone_name=normalized_timezone,
        run_hour=run_hour,
        run_minute=run_minute,
        weekdays=normalized_weekdays,
        cron_expr=normalized_cron,
    )
    return {
        'cadence': normalized_cadence,
        'timezone': normalized_timezone,
        'weekdays': normalized_weekdays,
        'run_hour': run_hour,
        'run_minute': run_minute,
        'cron_expr': normalized_cron,
    }


def _cron_matches(parts: list[str], local_dt: datetime) -> bool:
    minute_values = _parse_cron_field(parts[0], 0, 59)
    hour_values = _parse_cron_field(parts[1], 0, 23)
    dom_values = _parse_cron_field(parts[2], 1, 31)
    month_values = _parse_cron_field(parts[3], 1, 12)
    python_weekday = local_dt.weekday()
    cron_weekday = (python_weekday + 1) % 7
    dow_values = _parse_cron_field(parts[4], 0, 6)
    return (
        local_dt.minute in minute_values
        and local_dt.hour in hour_values
        and local_dt.day in dom_values
        and local_dt.month in month_values
        and cron_weekday in dow_values
    )


def calculate_next_run_at(
    *,
    cadence: str,
    timezone_name: str,
    run_hour: int | None,
    run_minute: int | None,
    weekdays: Iterable[int] | None,
    cron_expr: str | None,
    base_time: datetime | None = None,
) -> datetime:
    validate_schedule_payload(
        cadence=cadence,
        timezone_name=timezone_name,
        run_hour=run_hour,
        run_minute=run_minute,
        weekdays=weekdays,
        cron_expr=cron_expr,
    )
    tz = ZoneInfo(ensure_schedule_timezone(timezone_name))
    now_utc = (base_time or datetime.now(timezone.utc)).astimezone(timezone.utc)
    local_now = now_utc.astimezone(tz).replace(second=0, microsecond=0)
    normalized_cadence = ensure_schedule_cadence(cadence)

    if normalized_cadence == ScheduleCadence.DAILY.value:
        candidate = local_now.replace(hour=run_hour or 0, minute=run_minute or 0)
        if candidate <= local_now:
            candidate += timedelta(days=1)
        return candidate.astimezone(timezone.utc)

    if normalized_cadence == ScheduleCadence.WEEKLY.value:
        selected_days = normalize_weekdays(weekdays)
        for day_offset in range(0, 15):
            candidate_day = local_now + timedelta(days=day_offset)
            if candidate_day.weekday() not in selected_days:
                continue
            candidate = candidate_day.replace(hour=run_hour or 0, minute=run_minute or 0)
            if candidate > local_now:
                return candidate.astimezone(timezone.utc)
        raise ValueError('無法計算下一次每週排程時間')

    parts = validate_cron_expr(cron_expr).split()
    candidate = local_now + timedelta(minutes=1)
    for _ in range(0, 366 * 24 * 60):
        if _cron_matches(parts, candidate):
            return candidate.astimezone(timezone.utc)
        candidate += timedelta(minutes=1)
    raise ValueError('無法在一年內找到符合的 cron 執行時間')


def schedule_to_response(schedule: ScanSchedule) -> dict:
    weekdays = parse_weekdays(schedule.weekdays)
    return {
        'id': schedule.id,
        'asset_id': schedule.asset_id,
        'asset_name': schedule.asset.name if getattr(schedule, 'asset', None) else None,
        'name': schedule.name,
        'cadence': ensure_schedule_cadence(schedule.cadence),
        'cadence_label': CADENCE_LABELS.get(ensure_schedule_cadence(schedule.cadence), schedule.cadence),
        'timezone': schedule.timezone,
        'weekdays': weekdays,
        'weekdays_label': weekdays_label(weekdays),
        'run_hour': schedule.run_hour,
        'run_minute': schedule.run_minute,
        'cron_expr': schedule.cron_expr,
        'scan_profile': ensure_scan_profile(schedule.scan_profile),
        'device_template': ensure_template_key(schedule.device_template, schedule.asset.device_type if getattr(schedule, 'asset', None) else None),
        'credential_id': schedule.credential_id,
        'credential_name': schedule.credential.name if getattr(schedule, 'credential', None) else None,
        'is_active': schedule.is_active,
        'next_run_at': schedule.next_run_at,
        'last_run_at': schedule.last_run_at,
        'last_task_id': schedule.last_task_id,
        'last_error': schedule.last_error,
        'created_at': schedule.created_at,
        'updated_at': schedule.updated_at,
    }
