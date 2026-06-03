from collections.abc import Iterable
from datetime import datetime

from sqlalchemy import case, func, select
from sqlalchemy.ext.asyncio import AsyncSession

from src.models.asset import Asset, DeviceType
from src.models.scan import ScanTask
from src.models.vulnerability import Finding, FindingStatus
from src.services.scan_catalog import (
    get_device_template_definition,
    get_scan_profile_definition,
    normalize_device_template_key,
    normalize_scan_profile_key,
    recommended_template_for_device_type,
)


def normalize_tags(tags: Iterable[str] | None) -> str | None:
    if tags is None:
        return None

    normalized: list[str] = []
    seen: set[str] = set()
    for raw_tag in tags:
        tag = raw_tag.strip()
        if not tag:
            continue
        dedupe_key = tag.casefold()
        if dedupe_key in seen:
            continue
        seen.add(dedupe_key)
        normalized.append(tag)

    return ', '.join(normalized) if normalized else None


def parse_tags(raw_tags: str | None) -> list[str]:
    if not raw_tags:
        return []
    return [tag.strip() for tag in raw_tags.split(',') if tag.strip()]


def ensure_device_type(raw_device_type: str | None) -> DeviceType:
    if raw_device_type:
        for device_type in DeviceType:
            if device_type.value == raw_device_type:
                return device_type
    return DeviceType.COMPUTER


def ensure_scan_profile(raw_profile: str | None) -> str:
    return normalize_scan_profile_key(raw_profile)


def ensure_template_key(raw_template: str | None, raw_device_type: str | None = None) -> str:
    normalized = normalize_device_template_key(raw_template)
    if normalized != 'generic' or raw_template:
        return normalized
    return recommended_template_for_device_type(raw_device_type)


def latest_scan_timestamp(scan_task: ScanTask | None) -> datetime | None:
    if scan_task is None:
        return None
    return scan_task.finished_at or scan_task.started_at


async def build_asset_summary_map(
    session: AsyncSession,
    assets: list[Asset],
) -> dict[int, dict[str, object]]:
    asset_ids = [asset.id for asset in assets]
    if not asset_ids:
        return {}

    finding_counts_result = await session.execute(
        select(
            Finding.asset_id,
            func.count(Finding.id).label('total_findings'),
            func.sum(case((Finding.status == FindingStatus.OPEN, 1), else_=0)).label('open_findings'),
            func.sum(case((Finding.risk_score >= 15.0, 1), else_=0)).label('high_risk_findings'),
        )
        .where(Finding.asset_id.in_(asset_ids))
        .group_by(Finding.asset_id)
    )
    finding_counts = {
        row.asset_id: {
            'total_findings': int(row.total_findings or 0),
            'open_findings': int(row.open_findings or 0),
            'high_risk_findings': int(row.high_risk_findings or 0),
        }
        for row in finding_counts_result
    }

    latest_scan_subquery = (
        select(
            ScanTask.asset_id,
            func.max(ScanTask.id).label('latest_scan_id'),
        )
        .where(ScanTask.asset_id.in_(asset_ids))
        .group_by(ScanTask.asset_id)
        .subquery()
    )
    latest_scans_result = await session.execute(
        select(ScanTask).join(latest_scan_subquery, ScanTask.id == latest_scan_subquery.c.latest_scan_id)
    )
    latest_scans = {scan.asset_id: scan for scan in latest_scans_result.scalars().all()}

    summary_map: dict[int, dict[str, object]] = {}
    for asset in assets:
        latest_scan = latest_scans.get(asset.id)
        counts = finding_counts.get(asset.id, {})
        summary_map[asset.id] = {
            'last_scan_id': latest_scan.id if latest_scan else None,
            'last_scan_status': latest_scan.status if latest_scan else None,
            'last_scan_at': latest_scan_timestamp(latest_scan),
            'total_findings': counts.get('total_findings', 0),
            'open_findings': counts.get('open_findings', 0),
            'high_risk_findings': counts.get('high_risk_findings', 0),
        }

    return summary_map


def asset_to_response(asset: Asset, summary: dict[str, object] | None = None) -> dict[str, object]:
    summary = summary or {}
    return {
        'id': asset.id,
        'name': asset.name,
        'target': asset.target,
        'criticality': asset.criticality,
        'owner_id': asset.owner_id,
        'env': asset.env,
        'device_type': ensure_device_type(asset.device_type),
        'default_scan_profile': ensure_scan_profile(getattr(asset, 'default_scan_profile', None)),
        'template_key': ensure_template_key(getattr(asset, 'template_key', None), asset.device_type),
        'location': asset.location,
        'tags': parse_tags(asset.tags),
        'notes': asset.notes,
        'status': asset.status,
        'created_at': asset.created_at,
        'updated_at': asset.updated_at,
        'last_scan_id': summary.get('last_scan_id'),
        'last_scan_status': summary.get('last_scan_status'),
        'last_scan_at': summary.get('last_scan_at'),
        'total_findings': int(summary.get('total_findings', 0) or 0),
        'open_findings': int(summary.get('open_findings', 0) or 0),
        'high_risk_findings': int(summary.get('high_risk_findings', 0) or 0),
        'default_scan_profile_label': get_scan_profile_definition(getattr(asset, 'default_scan_profile', None)).label,
        'template_label': get_device_template_definition(getattr(asset, 'template_key', None) or recommended_template_for_device_type(asset.device_type)).label,
    }
