import unittest

from src.services.scan_processing import derive_vulnerability_key, normalize_severity


class ScanProcessingTests(unittest.TestCase):
    def test_normalize_severity_from_text(self):
        self.assertEqual(normalize_severity('critical'), 9.5)
        self.assertEqual(normalize_severity('medium'), 5.0)
        self.assertEqual(normalize_severity('unknown'), 0.0)

    def test_normalize_severity_from_numeric_string(self):
        self.assertEqual(normalize_severity('7.4'), 7.4)
        self.assertEqual(normalize_severity(11), 10.0)

    def test_derive_vulnerability_key_is_stable(self):
        item = {
            'template-id': 'cve-2026-0001',
            'matcher-name': 'default',
            'info': {'name': 'Sample Finding'},
        }

        self.assertEqual(
            derive_vulnerability_key(item),
            'cve-2026-0001|default|Sample Finding',
        )


if __name__ == '__main__':
    unittest.main()
