from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from src.core.security import decrypt_credential_value, encrypt_credential_value
from src.models.credential import Credential, CredentialKind


CREDENTIAL_KIND_LABELS = {
    CredentialKind.WINDOWS_PASSWORD.value: 'Windows 帳密',
    CredentialKind.LINUX_SSH_PASSWORD.value: 'Linux SSH 帳密',
    CredentialKind.LINUX_SSH_KEY.value: 'Linux SSH 金鑰',
    CredentialKind.SNMP_V2C.value: 'SNMP Community',
}


@dataclass(frozen=True)
class CredentialKindDefinition:
    key: str
    label: str
    description: str
    requires_username: bool
    requires_primary_secret: bool
    requires_secondary_secret: bool
    supports_domain: bool
    default_port: int | None


CREDENTIAL_KIND_DEFINITIONS: dict[str, CredentialKindDefinition] = {
    CredentialKind.WINDOWS_PASSWORD.value: CredentialKindDefinition(
        key=CredentialKind.WINDOWS_PASSWORD.value,
        label='Windows 帳密',
        description='用於 WinRM / SMB 等 Windows 認證型掃描前置資料。',
        requires_username=True,
        requires_primary_secret=True,
        requires_secondary_secret=False,
        supports_domain=True,
        default_port=5985,
    ),
    CredentialKind.LINUX_SSH_PASSWORD.value: CredentialKindDefinition(
        key=CredentialKind.LINUX_SSH_PASSWORD.value,
        label='Linux SSH 帳密',
        description='使用使用者名稱與密碼的 Linux SSH 認證資料。',
        requires_username=True,
        requires_primary_secret=True,
        requires_secondary_secret=False,
        supports_domain=False,
        default_port=22,
    ),
    CredentialKind.LINUX_SSH_KEY.value: CredentialKindDefinition(
        key=CredentialKind.LINUX_SSH_KEY.value,
        label='Linux SSH 金鑰',
        description='使用 SSH 私鑰的 Linux 認證資料，可選擇額外 passphrase。',
        requires_username=True,
        requires_primary_secret=True,
        requires_secondary_secret=True,
        supports_domain=False,
        default_port=22,
    ),
    CredentialKind.SNMP_V2C.value: CredentialKindDefinition(
        key=CredentialKind.SNMP_V2C.value,
        label='SNMP Community',
        description='用於網通設備 SNMP v2c 探測的 community 字串。',
        requires_username=False,
        requires_primary_secret=True,
        requires_secondary_secret=False,
        supports_domain=False,
        default_port=161,
    ),
}


def get_credential_kind_definition(kind: str) -> CredentialKindDefinition:
    if kind not in CREDENTIAL_KIND_DEFINITIONS:
        raise ValueError('不支援的 credential 類型')
    return CREDENTIAL_KIND_DEFINITIONS[kind]


def list_credential_kinds() -> list[dict[str, Any]]:
    return [
        {
            'key': definition.key,
            'label': definition.label,
            'description': definition.description,
            'requires_username': definition.requires_username,
            'requires_primary_secret': definition.requires_primary_secret,
            'requires_secondary_secret': definition.requires_secondary_secret,
            'supports_domain': definition.supports_domain,
            'default_port': definition.default_port,
        }
        for definition in CREDENTIAL_KIND_DEFINITIONS.values()
    ]


def validate_credential_payload(
    *,
    kind: str,
    username: str | None,
    primary_secret: str | None,
    secondary_secret: str | None,
) -> None:
    definition = get_credential_kind_definition(kind)
    if definition.requires_username and not (username or '').strip():
        raise ValueError('此 credential 類型需要使用者名稱')
    if definition.requires_primary_secret and not (primary_secret or '').strip():
        raise ValueError('此 credential 類型需要主要密鑰或密碼')
    if definition.requires_secondary_secret and not (secondary_secret or '').strip():
        raise ValueError('此 credential 類型需要第二密鑰或 passphrase')


def credential_to_response(credential: Credential) -> dict[str, Any]:
    definition = get_credential_kind_definition(credential.kind)
    return {
        'id': credential.id,
        'name': credential.name,
        'kind': credential.kind,
        'kind_label': definition.label,
        'owner_id': credential.owner_id,
        'username': credential.username,
        'domain': credential.domain,
        'port': credential.port,
        'notes': credential.notes,
        'is_active': credential.is_active,
        'has_primary_secret': credential.secret_ciphertext is not None,
        'has_secondary_secret': credential.secondary_secret_ciphertext is not None,
        'last_used_at': credential.last_used_at,
        'created_at': credential.created_at,
        'updated_at': credential.updated_at,
    }


def apply_credential_secrets(
    credential: Credential,
    *,
    primary_secret: str | None,
    secondary_secret: str | None,
) -> None:
    if primary_secret is not None:
        credential.secret_ciphertext = encrypt_credential_value(primary_secret)
    if secondary_secret is not None:
        credential.secondary_secret_ciphertext = encrypt_credential_value(secondary_secret)


def build_credential_runtime_context(credential: Credential | None) -> dict[str, Any] | None:
    if credential is None:
        return None

    primary_secret = decrypt_credential_value(credential.secret_ciphertext)
    secondary_secret = decrypt_credential_value(credential.secondary_secret_ciphertext)
    return {
        'id': credential.id,
        'name': credential.name,
        'kind': credential.kind,
        'username': credential.username,
        'domain': credential.domain,
        'port': credential.port,
        'primary_secret': primary_secret,
        'secondary_secret': secondary_secret,
    }
