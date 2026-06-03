import unittest

from src.core.security import decrypt_credential_value, encrypt_credential_value
from src.services.credential_service import (
    get_credential_kind_definition,
    validate_credential_payload,
)
from src.services.scan_catalog import credential_kind_supported_by_profile, profile_requires_credential


class CredentialServiceTests(unittest.TestCase):
    def test_encrypt_and_decrypt_roundtrip(self):
        ciphertext = encrypt_credential_value('SecretValue123!')
        self.assertIsNotNone(ciphertext)
        self.assertNotEqual(ciphertext, 'SecretValue123!')
        self.assertEqual(decrypt_credential_value(ciphertext), 'SecretValue123!')

    def test_windows_credential_requires_username_and_password(self):
        with self.assertRaises(ValueError):
            validate_credential_payload(
                kind='WindowsPassword',
                username='',
                primary_secret='Password123!',
                secondary_secret=None,
            )
        with self.assertRaises(ValueError):
            validate_credential_payload(
                kind='WindowsPassword',
                username='svc_scanner',
                primary_secret='',
                secondary_secret=None,
            )

    def test_linux_ssh_key_requires_secondary_secret(self):
        with self.assertRaises(ValueError):
            validate_credential_payload(
                kind='LinuxSSHKey',
                username='scanner',
                primary_secret='-----BEGIN OPENSSH PRIVATE KEY-----',
                secondary_secret='',
            )

    def test_profile_credential_compatibility(self):
        self.assertTrue(profile_requires_credential('authenticated_windows'))
        self.assertTrue(credential_kind_supported_by_profile('authenticated_windows', 'WindowsPassword'))
        self.assertFalse(credential_kind_supported_by_profile('authenticated_windows', 'SNMPv2c'))
        self.assertTrue(credential_kind_supported_by_profile('standard', None))

    def test_kind_definition_contains_labels(self):
        definition = get_credential_kind_definition('SNMPv2c')
        self.assertEqual(definition.label, 'SNMP Community')


if __name__ == '__main__':
    unittest.main()
