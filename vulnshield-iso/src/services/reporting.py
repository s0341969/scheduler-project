from __future__ import annotations

from collections import Counter

from src.models.asset import Asset
from src.models.scan import ScanStatus, ScanTask
from src.models.vulnerability import Finding
from src.services.asset_inventory import ensure_asset_status


ASSET_STATUS_LABELS = {
    'Active': '運作中',
    'Maintenance': '維護中',
    'Retired': '已退役',
}


def build_priority_assets(assets: list[Asset], findings: list[Finding], limit: int = 6) -> list[dict]:
    finding_by_asset: dict[int, list[Finding]] = {}
    for finding in findings:
        finding_by_asset.setdefault(finding.asset_id, []).append(finding)

    ranked = []
    for asset in assets:
        asset_findings = finding_by_asset.get(asset.id, [])
        open_findings = sum(1 for finding in asset_findings if getattr(finding.status, 'value', finding.status) in {'Open', 'Acknowledged'})
        high_risk = sum(1 for finding in asset_findings if (finding.risk_score or 0) >= 15.0)
        risk_total = sum(float(finding.risk_score or 0) for finding in asset_findings)
        ranked.append(
            {
                'asset_id': asset.id,
                'name': asset.name,
                'target': asset.target,
                'device_type': asset.device_type,
                'status': ensure_asset_status(asset.status),
                'criticality': asset.criticality,
                'open_findings': open_findings,
                'high_risk_findings': high_risk,
                'risk_total': round(risk_total, 2),
                'priority_score': round((high_risk * 20) + (open_findings * 5) + risk_total + (asset.criticality * 3), 2),
            }
        )
    return sorted(ranked, key=lambda item: item['priority_score'], reverse=True)[:limit]


def build_recommendations(findings: list[Finding], scans: list[ScanTask], assets: list[Asset]) -> list[dict]:
    recommendations: list[dict] = []
    high_risk = sum(1 for finding in findings if (finding.risk_score or 0) >= 15.0)
    failed_scans = sum(1 for scan in scans if scan.status == ScanStatus.FAILED)
    pending_scans = sum(1 for scan in scans if scan.status in {ScanStatus.PENDING, ScanStatus.RUNNING})
    retired_assets = [asset for asset in assets if ensure_asset_status(asset.status) == 'Retired']

    if high_risk > 0:
        recommendations.append(
            {
                'title': '優先處理高風險弱點',
                'severity': 'high',
                'detail': f'目前共有 {high_risk} 筆高風險 finding，應先處理外網資產與高重要度設備。',
            }
        )
    if failed_scans > 0:
        recommendations.append(
            {
                'title': '修復失敗掃描任務',
                'severity': 'medium',
                'detail': f'目前有 {failed_scans} 筆掃描失敗，建議先檢查網路可達性、credential 或掃描模板。',
            }
        )
    if pending_scans > 3:
        recommendations.append(
            {
                'title': '清理待處理掃描佇列',
                'severity': 'medium',
                'detail': f'目前有 {pending_scans} 筆待處理或執行中的任務，建議檢查 worker 容量與掃描節奏。',
            }
        )
    if retired_assets:
        recommendations.append(
            {
                'title': '確認退役設備是否仍需保留',
                'severity': 'low',
                'detail': f'目前有 {len(retired_assets)} 台設備標記為退役，建議確認是否仍需保留 inventory 或 finding。',
            }
        )
    if not recommendations:
        recommendations.append(
            {
                'title': '目前風險在可控範圍',
                'severity': 'info',
                'detail': '目前沒有高風險 finding 或異常掃描堆積，可持續維持定期掃描與驗證流程。',
            }
        )
    return recommendations


def build_report_snapshot(assets: list[Asset], findings: list[Finding], scans: list[ScanTask]) -> dict:
    high_risk = len([f for f in findings if f.risk_score is not None and f.risk_score >= 15.0])
    med_risk = len([f for f in findings if f.risk_score is not None and 5.0 <= f.risk_score < 15.0])
    low_risk = len([f for f in findings if f.risk_score is not None and f.risk_score < 5.0])

    scan_layers = {
        'services': 0,
        'vulnerabilities': 0,
        'misconfigurations': 0,
        'certificate_risks': 0,
        'exposures': 0,
        'informational': 0,
    }
    scan_status = {
        ScanStatus.PENDING.value: 0,
        ScanStatus.RUNNING.value: 0,
        ScanStatus.COMPLETED.value: 0,
        ScanStatus.FAILED.value: 0,
    }
    profile_distribution: dict[str, int] = {}

    for scan in scans:
        scan_status[scan.status.value] = scan_status.get(scan.status.value, 0) + 1
        profile_distribution[scan.scan_profile] = profile_distribution.get(scan.scan_profile, 0) + 1

        summary = scan.scan_summary or {}
        scan_layers['services'] += int(summary.get('service_count', 0) or 0)
        scan_layers['vulnerabilities'] += int(summary.get('vulnerability_count', 0) or 0)
        scan_layers['misconfigurations'] += int(summary.get('misconfiguration_count', 0) or 0)
        scan_layers['certificate_risks'] += int(summary.get('certificate_risk_count', 0) or 0)
        scan_layers['exposures'] += int(summary.get('exposure_count', 0) or 0)
        scan_layers['informational'] += int(summary.get('informational_count', 0) or 0)

    asset_status_distribution = Counter(ensure_asset_status(asset.status) for asset in assets)

    return {
        'summary': {
            'total_findings': len(findings),
            'high_risk': high_risk,
            'medium_risk': med_risk,
            'low_risk': low_risk,
        },
        'scan_layers': scan_layers,
        'scan_status': scan_status,
        'profile_distribution': profile_distribution,
        'asset_status_distribution': dict(asset_status_distribution),
        'priority_assets': build_priority_assets(assets, findings),
        'recommendations': build_recommendations(findings, scans, assets),
        'compliance_status': 'In Progress' if high_risk > 0 else 'Compliant',
    }
