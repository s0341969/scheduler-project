from datetime import datetime, timezone
from typing import Any

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from src.models.asset import Asset
from src.models.vulnerability import Finding, FindingStatus, Vulnerability
from src.services.risk_calculator import calculate_risk_score


SEVERITY_SCORE_MAP = {
    'info': 0.0,
    'low': 2.0,
    'medium': 5.0,
    'high': 8.0,
    'critical': 9.5,
}


def normalize_severity(raw_severity: Any) -> float:
    if isinstance(raw_severity, (int, float)):
        return max(0.0, min(float(raw_severity), 10.0))

    if isinstance(raw_severity, str):
        normalized = raw_severity.strip().lower()
        if normalized in SEVERITY_SCORE_MAP:
            return SEVERITY_SCORE_MAP[normalized]
        try:
            return max(0.0, min(float(normalized), 10.0))
        except ValueError:
            return 0.0

    return 0.0


def derive_vulnerability_key(item: dict[str, Any]) -> str:
    template_id = str(item.get('template-id') or '').strip()
    matcher_name = str(item.get('matcher-name') or '').strip()
    name = str(item.get('info', {}).get('name') or '').strip()
    key = '|'.join(part for part in (template_id, matcher_name, name) if part)
    return key or 'unknown-vulnerability'


async def get_or_create_vulnerability(
    session: AsyncSession,
    item: dict[str, Any],
) -> Vulnerability:
    vulnerability_key = derive_vulnerability_key(item)
    result = await session.execute(
        select(Vulnerability).where(Vulnerability.cve_id == vulnerability_key)
    )
    vulnerability = result.scalar_one_or_none()
    severity = normalize_severity(item.get('info', {}).get('severity'))

    if vulnerability is None:
        vulnerability = Vulnerability(
            cve_id=vulnerability_key,
            title=item.get('info', {}).get('name', '未知弱點'),
            description=item.get('info', {}).get('description', ''),
            severity=severity,
            remediation='請依掃描結果與系統修補公告進行修復',
        )
        session.add(vulnerability)
        await session.flush()
        return vulnerability

    vulnerability.title = item.get('info', {}).get('name', vulnerability.title)
    vulnerability.description = item.get('info', {}).get('description', vulnerability.description)
    vulnerability.severity = severity
    return vulnerability


async def upsert_finding(
    session: AsyncSession,
    asset: Asset,
    vulnerability: Vulnerability,
) -> Finding:
    result = await session.execute(
        select(Finding).where(
            Finding.asset_id == asset.id,
            Finding.vuln_id == vulnerability.id,
        )
    )
    finding = result.scalar_one_or_none()
    risk_score = calculate_risk_score(vulnerability.severity, asset.criticality)
    now = datetime.now(timezone.utc)

    if finding is None:
        finding = Finding(
            asset_id=asset.id,
            vuln_id=vulnerability.id,
            risk_score=risk_score,
            status=FindingStatus.OPEN,
        )
        session.add(finding)
        await session.flush()
        return finding

    finding.risk_score = risk_score
    finding.last_seen = now
    if finding.status == FindingStatus.VERIFIED:
        finding.status = FindingStatus.OPEN
    return finding
