import unittest

from src.models.asset import DeviceType
from src.services.asset_inventory import ensure_device_type, normalize_tags, parse_tags


class AssetInventoryTests(unittest.TestCase):
    def test_normalize_tags_deduplicates_and_trims(self):
        normalized = normalize_tags([' 總部 ', '外網', '總部', '', '防火牆 '])
        self.assertEqual(normalized, '總部, 外網, 防火牆')

    def test_parse_tags_returns_list(self):
        self.assertEqual(parse_tags('總部, 外網, 防火牆'), ['總部', '外網', '防火牆'])
        self.assertEqual(parse_tags(None), [])

    def test_ensure_device_type_falls_back_to_computer(self):
        self.assertEqual(ensure_device_type('Firewall'), DeviceType.FIREWALL)
        self.assertEqual(ensure_device_type('UnknownType'), DeviceType.COMPUTER)
        self.assertEqual(ensure_device_type(None), DeviceType.COMPUTER)


if __name__ == '__main__':
    unittest.main()
