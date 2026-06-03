from __future__ import annotations

from dataclasses import asdict, dataclass

from src.models.asset import DeviceType


LEGACY_PROFILE_ALIASES = {
    'full': 'standard',
}


@dataclass(frozen=True)
class ScanProfileDefinition:
    key: str
    label: str
    description: str
    scope: str
    intensity: str
    recommended_for: tuple[str, ...]
    run_nmap: bool
    run_nuclei: bool
    nmap_args: tuple[str, ...]
    nuclei_tags: tuple[str, ...]
    max_duration_seconds: int
    authenticated: bool = False
    required_credential_kinds: tuple[str, ...] = ()


@dataclass(frozen=True)
class DeviceTemplateDefinition:
    key: str
    label: str
    description: str
    recommended_profile: str
    device_types: tuple[str, ...]
    nuclei_tags: tuple[str, ...]
    management_ports: tuple[int, ...]


SCAN_PROFILE_DEFINITIONS: dict[str, ScanProfileDefinition] = {
    'quick': ScanProfileDefinition(
        key='quick',
        label='快速掃描',
        description='先用常見通訊埠與低噪音模板快速判斷是否存在可疑暴露面。',
        scope='快速盤點',
        intensity='低',
        recommended_for=('初次盤點', '日常追蹤'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-F', '-sV', '-T4'),
        nuclei_tags=('exposure', 'ssl', 'tech'),
        max_duration_seconds=240,
    ),
    'standard': ScanProfileDefinition(
        key='standard',
        label='標準掃描',
        description='平衡速度與覆蓋率，適合作為大多數設備的預設掃描模式。',
        scope='標準弱掃',
        intensity='中',
        recommended_for=('伺服器', '防火牆', 'NAS', '一般設備'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-sV', '-T4'),
        nuclei_tags=(),
        max_duration_seconds=600,
    ),
    'aggressive': ScanProfileDefinition(
        key='aggressive',
        label='高強度掃描',
        description='使用全埠與較深入的探測參數，適合維護時段或進階稽核。',
        scope='深入稽核',
        intensity='高',
        recommended_for=('核心系統', '定期深度檢查'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-p-', '-sV', '-sC', '-T4'),
        nuclei_tags=(),
        max_duration_seconds=1200,
    ),
    'web_only': ScanProfileDefinition(
        key='web_only',
        label='Web-only',
        description='聚焦 Web 面向、憑證與管理介面暴露，適合 Web Server 與具 Web UI 的設備。',
        scope='Web 面向',
        intensity='中',
        recommended_for=('Web Server', 'NAS', 'Firewall UI'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-Pn', '-sV', '-p', '80,81,443,444,591,593,8000,8008,8080,8081,8088,8443,8888,9443'),
        nuclei_tags=('http', 'ssl', 'tech', 'exposure', 'misconfig', 'panel'),
        max_duration_seconds=720,
    ),
    'network_only': ScanProfileDefinition(
        key='network_only',
        label='Network-only',
        description='偏重網路服務與管理面暴露，不執行 Web 導向模板。',
        scope='網路面向',
        intensity='中',
        recommended_for=('交換器', '路由器', '內網設備'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-sV', '-O', '-T4'),
        nuclei_tags=('network', 'ssl', 'exposure'),
        max_duration_seconds=720,
    ),
    'authenticated_windows': ScanProfileDefinition(
        key='authenticated_windows',
        label='Authenticated Windows',
        description='綁定 Windows 帳密做前置檢查，並針對 WinRM / SMB / RDP 面向加強盤點。',
        scope='認證型 Windows',
        intensity='高',
        recommended_for=('Windows Server', '域內主機'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-Pn', '-sV', '-p', '135,139,445,3389,5985'),
        nuclei_tags=('network', 'ssl', 'exposure', 'windows'),
        max_duration_seconds=900,
        authenticated=True,
        required_credential_kinds=('WindowsPassword',),
    ),
    'authenticated_linux': ScanProfileDefinition(
        key='authenticated_linux',
        label='Authenticated Linux',
        description='綁定 Linux SSH 認證資料，聚焦 SSH 與常見 Linux 管理面前置檢查。',
        scope='認證型 Linux',
        intensity='高',
        recommended_for=('Linux Server', '應用主機'),
        run_nmap=True,
        run_nuclei=True,
        nmap_args=('-Pn', '-sV', '-p', '22,80,443,8080,8443'),
        nuclei_tags=('network', 'ssl', 'exposure', 'linux'),
        max_duration_seconds=900,
        authenticated=True,
        required_credential_kinds=('LinuxSSHPassword', 'LinuxSSHKey'),
    ),
    'authenticated_snmp': ScanProfileDefinition(
        key='authenticated_snmp',
        label='Authenticated SNMP',
        description='綁定 SNMP community，對網通設備執行 SNMP 前置檢查。',
        scope='認證型 SNMP',
        intensity='中',
        recommended_for=('交換器', '路由器', '網通設備'),
        run_nmap=True,
        run_nuclei=False,
        nmap_args=('-sU', '-Pn', '-p', '161,162'),
        nuclei_tags=(),
        max_duration_seconds=600,
        authenticated=True,
        required_credential_kinds=('SNMPv2c',),
    ),
}


DEVICE_TEMPLATE_DEFINITIONS: dict[str, DeviceTemplateDefinition] = {
    'generic': DeviceTemplateDefinition(
        key='generic',
        label='通用模板',
        description='適合一般電腦、伺服器與尚未明確分類的設備。',
        recommended_profile='standard',
        device_types=(DeviceType.COMPUTER.value, DeviceType.SERVER.value, DeviceType.OTHER.value),
        nuclei_tags=('exposure', 'ssl'),
        management_ports=(22, 80, 443, 3389, 5900),
    ),
    'firewall': DeviceTemplateDefinition(
        key='firewall',
        label='防火牆模板',
        description='強化管理介面、憑證、外露入口與已知設備風險比對。',
        recommended_profile='standard',
        device_types=(DeviceType.FIREWALL.value,),
        nuclei_tags=('network', 'panel', 'ssl', 'exposure', 'firewall'),
        management_ports=(22, 80, 443, 8443, 9443),
    ),
    'switch': DeviceTemplateDefinition(
        key='switch',
        label='交換器模板',
        description='聚焦 SNMP、管理頁、遠端登入服務與網通設備暴露。',
        recommended_profile='network_only',
        device_types=(DeviceType.SWITCH.value, DeviceType.ROUTER.value, DeviceType.NETWORK_DEVICE.value),
        nuclei_tags=('network', 'snmp', 'ssl', 'exposure'),
        management_ports=(22, 23, 80, 443, 161, 162),
    ),
    'nas': DeviceTemplateDefinition(
        key='nas',
        label='NAS 模板',
        description='聚焦 NAS 管理頁、檔案服務、憑證與產品特徵風險。',
        recommended_profile='standard',
        device_types=(DeviceType.NAS.value,),
        nuclei_tags=('panel', 'ssl', 'exposure', 'network', 'nas'),
        management_ports=(80, 443, 5000, 5001, 8080, 8443),
    ),
    'web_server': DeviceTemplateDefinition(
        key='web_server',
        label='Web Server 模板',
        description='聚焦 Web 技術指紋、錯誤設定、憑證與常見 Web 暴露。',
        recommended_profile='web_only',
        device_types=(DeviceType.SERVER.value,),
        nuclei_tags=('http', 'tech', 'misconfig', 'ssl', 'exposure', 'cve'),
        management_ports=(80, 443, 8080, 8443, 9443),
    ),
}


def normalize_scan_profile_key(raw_profile: str | None) -> str:
    if not raw_profile:
        return 'standard'
    normalized = str(raw_profile).strip().lower()
    normalized = LEGACY_PROFILE_ALIASES.get(normalized, normalized)
    return normalized if normalized in SCAN_PROFILE_DEFINITIONS else 'standard'


def normalize_device_template_key(raw_template: str | None) -> str:
    if not raw_template:
        return 'generic'
    normalized = str(raw_template).strip().lower()
    return normalized if normalized in DEVICE_TEMPLATE_DEFINITIONS else 'generic'


def recommended_template_for_device_type(device_type: str | DeviceType | None) -> str:
    resolved = device_type.value if isinstance(device_type, DeviceType) else str(device_type or DeviceType.COMPUTER.value)
    for definition in DEVICE_TEMPLATE_DEFINITIONS.values():
        if resolved in definition.device_types:
            return definition.key
    return 'generic'


def recommended_profile_for_template(template_key: str | None) -> str:
    definition = get_device_template_definition(template_key)
    return definition.key and definition.recommended_profile


def get_scan_profile_definition(profile_key: str | None) -> ScanProfileDefinition:
    normalized = normalize_scan_profile_key(profile_key)
    return SCAN_PROFILE_DEFINITIONS[normalized]


def get_device_template_definition(template_key: str | None) -> DeviceTemplateDefinition:
    normalized = normalize_device_template_key(template_key)
    return DEVICE_TEMPLATE_DEFINITIONS[normalized]


def list_scan_profiles() -> list[dict[str, object]]:
    return [asdict(definition) for definition in SCAN_PROFILE_DEFINITIONS.values()]


def list_device_templates() -> list[dict[str, object]]:
    return [asdict(definition) for definition in DEVICE_TEMPLATE_DEFINITIONS.values()]


def build_scan_execution_plan(profile_key: str | None, template_key: str | None) -> dict[str, object]:
    profile = get_scan_profile_definition(profile_key)
    template = get_device_template_definition(template_key)

    merged_tags = list(dict.fromkeys([*profile.nuclei_tags, *template.nuclei_tags]))
    return {
        'profile': asdict(profile),
        'device_template': asdict(template),
        'engines': {
            'nmap': profile.run_nmap,
            'nuclei': profile.run_nuclei,
        },
        'nmap_args': list(profile.nmap_args),
        'nuclei_tags': merged_tags,
        'max_duration_seconds': profile.max_duration_seconds,
        'authenticated': profile.authenticated,
        'required_credential_kinds': list(profile.required_credential_kinds),
    }


def build_redacted_credential_metadata(credential: dict[str, object] | None) -> dict[str, object] | None:
    if credential is None:
        return None
    return {
        'id': credential.get('id'),
        'name': credential.get('name'),
        'kind': credential.get('kind'),
        'username': credential.get('username'),
        'domain': credential.get('domain'),
        'port': credential.get('port'),
    }


def profile_requires_credential(profile_key: str | None) -> bool:
    return get_scan_profile_definition(profile_key).authenticated


def credential_kind_supported_by_profile(profile_key: str | None, credential_kind: str | None) -> bool:
    profile = get_scan_profile_definition(profile_key)
    if not profile.required_credential_kinds:
        return credential_kind is None
    return credential_kind in profile.required_credential_kinds
