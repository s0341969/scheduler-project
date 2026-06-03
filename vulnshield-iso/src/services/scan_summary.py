import re
from typing import Any

from src.services.scan_processing import normalize_severity


SERVICE_LINE_PATTERN = re.compile(
    r'^(?P<port>\d+)\/(?P<protocol>\w+)\s+'
    r'(?P<state>\S+)\s+'
    r'(?P<service>\S+)'
    r'(?:\s+(?P<product>.+))?$'
)


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


def summarize_scan_results(
    nmap_results: list[dict[str, Any]],
    nuclei_results: list[dict[str, Any]],
) -> dict[str, Any]:
    services = parse_nmap_services(nmap_results)
    vulnerabilities: list[dict[str, str | None]] = []
    informational: list[dict[str, str | None]] = []

    for item in nuclei_results:
        signal = _build_signal(item)
        score = normalize_severity((item.get('info', {}) or {}).get('severity'))
        if score <= 0.0 or signal['severity'] == 'info':
            informational.append(signal)
        else:
            vulnerabilities.append(signal)

    engines = [
        {
            'name': 'Nmap',
            'status': 'completed' if nmap_results else 'no-open-service-detected',
            'detail': '服務與版本探測',
        },
        {
            'name': 'Nuclei',
            'status': 'completed' if nuclei_results else 'no-template-match',
            'detail': '弱點模板比對與風險提示',
        },
    ]

    return {
        'engines': engines,
        'services': services,
        'vulnerabilities': vulnerabilities,
        'informational': informational,
        'service_count': len(services),
        'vulnerability_count': len(vulnerabilities),
        'informational_count': len(informational),
    }
