import re
from typing import Any

from src.services.scan_processing import normalize_severity
from src.services.scan_catalog import build_scan_execution_plan


SERVICE_LINE_PATTERN = re.compile(
    r'^(?P<port>\d+)\/(?P<protocol>\w+)\s+'
    r'(?P<state>\S+)\s+'
    r'(?P<service>\S+)'
    r'(?:\s+(?P<product>.+))?$'
)

WEB_SERVICES = {'http', 'https', 'http-proxy'}
MANAGEMENT_SERVICE_NAMES = {
    'ssh',
    'telnet',
    'rdp',
    'vnc',
    'winrm',
    'msrpc',
    'snmp',
    'http',
    'https',
    'smb',
}
MANAGEMENT_PORTS = {22, 23, 80, 135, 139, 161, 162, 389, 443, 445, 3389, 5900, 5985, 8080, 8443, 9443}


def parse_nmap_services(nmap_results: list[dict[str, Any]]) -> list[dict[str, str | None]]:
    services: list[dict[str, str | None]] = []
    for item in nmap_results:
        output = str(item.get('output') or '')
        for raw_line in output.splitlines():
            line = raw_line.strip()
            match = SERVICE_LINE_PATTERN.match(line)
            if not match:
                continue
            services.append(
                {
                    'port': match.group('port'),
                    'protocol': match.group('protocol'),
                    'state': match.group('state'),
                    'service': match.group('service'),
                    'product': match.group('product'),
                }
            )
    return services


def _build_signal(item: dict[str, Any]) -> dict[str, str | None]:
    info = item.get('info', {}) or {}
    severity = str(info.get('severity') or 'info').strip().lower() or 'info'
    return {
        'title': str(info.get('name') or '未知結果'),
        'severity': severity,
        'template_id': str(item.get('template-id') or '') or None,
        'matcher_name': str(item.get('matcher-name') or '') or None,
        'matched_at': str(item.get('matched-at') or '') or None,
        'description': str(info.get('description') or '') or None,
    }


def _combined_keywords(item: dict[str, Any]) -> str:
    info = item.get('info', {}) or {}
    pieces = [
        str(item.get('template-id') or ''),
        str(item.get('matcher-name') or ''),
        str(item.get('matched-at') or ''),
        str(info.get('name') or ''),
        str(info.get('description') or ''),
        ' '.join(str(tag) for tag in (info.get('tags') or [])),
    ]
    return ' '.join(piece.strip().lower() for piece in pieces if piece)


def _classify_nuclei_signal(item: dict[str, Any], signal: dict[str, str | None]) -> str:
    keywords = _combined_keywords(item)
    severity_score = normalize_severity((item.get('info', {}) or {}).get('severity'))

    if any(keyword in keywords for keyword in ('tls', 'ssl', 'certificate', 'x509', 'cipher', 'expired cert')):
        return 'certificate_risks'

    if any(keyword in keywords for keyword in ('default-login', 'misconfig', 'directory listing', 'exposed config', 'open redirect', 'cors', 'security header')):
        return 'misconfigurations'

    if any(keyword in keywords for keyword in ('panel', 'dashboard', 'console', 'admin', 'login', 'manager', 'rdp', 'vnc', 'winrm', 'snmp', 'exposure', 'exposed')):
        return 'exposures'

    if severity_score <= 0.0 or signal['severity'] == 'info':
        return 'informational'

    return 'vulnerabilities'


def _service_to_exposure_signal(service: dict[str, str | None]) -> dict[str, str | None] | None:
    service_name = str(service.get('service') or '').strip().lower()
    port_value = str(service.get('port') or '').strip()
    try:
        port = int(port_value)
    except ValueError:
        port = -1

    if service_name not in MANAGEMENT_SERVICE_NAMES and port not in MANAGEMENT_PORTS:
        return None

    title = '管理介面或遠端服務暴露'
    description = f"發現 {port_value}/{service.get('protocol')} {service_name or 'unknown'} 對外可達，建議確認是否需要限制來源或加強驗證。"
    if service_name in WEB_SERVICES or port in {80, 443, 8080, 8443, 9443}:
        title = 'Web 管理介面可能暴露'
    elif port in {3389, 5900, 5985, 22, 23}:
        title = '遠端管理服務可能暴露'

    return {
        'title': title,
        'severity': 'info',
        'template_id': 'service-exposure',
        'matcher_name': service_name or 'service-detect',
        'matched_at': f"{port_value}/{service.get('protocol')}",
        'description': description,
    }


def summarize_scan_results(
    nmap_results: list[dict[str, Any]],
    nuclei_results: list[dict[str, Any]],
    *,
    profile_key: str = 'standard',
    device_template_key: str = 'generic',
) -> dict[str, Any]:
    services = parse_nmap_services(nmap_results)
    plan = build_scan_execution_plan(profile_key, device_template_key)
    vulnerabilities: list[dict[str, str | None]] = []
    misconfigurations: list[dict[str, str | None]] = []
    certificate_risks: list[dict[str, str | None]] = []
    exposures: list[dict[str, str | None]] = []
    informational: list[dict[str, str | None]] = []

    for item in nuclei_results:
        signal = _build_signal(item)
        category = _classify_nuclei_signal(item, signal)
        if category == 'certificate_risks':
            certificate_risks.append(signal)
        elif category == 'misconfigurations':
            misconfigurations.append(signal)
        elif category == 'exposures':
            exposures.append(signal)
        elif category == 'informational':
            informational.append(signal)
        else:
            vulnerabilities.append(signal)

    for service in services:
        exposure_signal = _service_to_exposure_signal(service)
        if exposure_signal is not None:
            exposures.append(exposure_signal)

    engines = [
        {
            'name': 'Nmap',
            'status': 'completed' if plan['engines']['nmap'] and nmap_results else 'skipped' if not plan['engines']['nmap'] else 'no-open-service-detected',
            'detail': '服務與版本探測',
            'scope': plan['profile']['scope'],
        },
        {
            'name': 'Nuclei',
            'status': 'completed' if plan['engines']['nuclei'] and nuclei_results else 'skipped' if not plan['engines']['nuclei'] else 'no-template-match',
            'detail': '弱點模板比對與風險提示',
            'scope': plan['device_template']['label'],
        },
    ]

    return {
        'profile_key': plan['profile']['key'],
        'profile_label': plan['profile']['label'],
        'profile_scope': plan['profile']['scope'],
        'device_template_key': plan['device_template']['key'],
        'device_template_label': plan['device_template']['label'],
        'engines': engines,
        'services': services,
        'vulnerabilities': vulnerabilities,
        'misconfigurations': misconfigurations,
        'certificate_risks': certificate_risks,
        'exposures': exposures,
        'informational': informational,
        'service_count': len(services),
        'vulnerability_count': len(vulnerabilities),
        'misconfiguration_count': len(misconfigurations),
        'certificate_risk_count': len(certificate_risks),
        'exposure_count': len(exposures),
        'informational_count': len(informational),
        'actionable_signal_count': len(vulnerabilities) + len(misconfigurations) + len(certificate_risks) + len(exposures),
    }


def normalize_scan_summary_payload(
    raw_summary: dict[str, Any] | None,
    *,
    profile_key: str = 'standard',
    device_template_key: str = 'generic',
) -> dict[str, Any] | None:
    if raw_summary is None:
        return None

    plan = build_scan_execution_plan(profile_key, device_template_key)
    normalized = dict(raw_summary)
    normalized.setdefault('profile_key', plan['profile']['key'])
    normalized.setdefault('profile_label', plan['profile']['label'])
    normalized.setdefault('profile_scope', plan['profile']['scope'])
    normalized.setdefault('device_template_key', plan['device_template']['key'])
    normalized.setdefault('device_template_label', plan['device_template']['label'])
    normalized.setdefault('engines', [])
    normalized.setdefault('services', [])
    normalized.setdefault('vulnerabilities', [])
    normalized.setdefault('misconfigurations', [])
    normalized.setdefault('certificate_risks', [])
    normalized.setdefault('exposures', [])
    normalized.setdefault('informational', [])
    normalized.setdefault('service_count', len(normalized['services']))
    normalized.setdefault('vulnerability_count', len(normalized['vulnerabilities']))
    normalized.setdefault('misconfiguration_count', len(normalized['misconfigurations']))
    normalized.setdefault('certificate_risk_count', len(normalized['certificate_risks']))
    normalized.setdefault('exposure_count', len(normalized['exposures']))
    normalized.setdefault('informational_count', len(normalized['informational']))
    normalized.setdefault(
        'actionable_signal_count',
        int(normalized['vulnerability_count'])
        + int(normalized['misconfiguration_count'])
        + int(normalized['certificate_risk_count'])
        + int(normalized['exposure_count']),
    )
    return normalized
