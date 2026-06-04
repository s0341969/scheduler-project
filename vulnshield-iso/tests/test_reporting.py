import unittest
from types import SimpleNamespace

from src.models.scan import ScanStatus
from src.models.vulnerability import FindingStatus
from src.services.reporting import build_report_snapshot


class ReportingTests(unittest.TestCase):
    def test_build_report_snapshot_includes_priority_and_recommendations(self):
        assets = [
            SimpleNamespace(id=1, name='FW-1', target='10.0.0.1', device_type='Firewall', status='Active', criticality=5),
            SimpleNamespace(id=2, name='Legacy-NAS', target='10.0.0.8', device_type='NAS', status='Retired', criticality=2),
        ]
        findings = [
            SimpleNamespace(asset_id=1, risk_score=18.0, status=FindingStatus.OPEN),
            SimpleNamespace(asset_id=1, risk_score=9.0, status=FindingStatus.ACKNOWLEDGED),
        ]
        scans = [
            SimpleNamespace(
                status=ScanStatus.FAILED,
                scan_profile='standard',
                scan_summary={
                    'service_count': 2,
                    'vulnerability_count': 1,
                    'misconfiguration_count': 1,
                    'certificate_risk_count': 0,
                    'exposure_count': 0,
                    'informational_count': 1,
                },
            )
        ]

        snapshot = build_report_snapshot(assets, findings, scans)

        self.assertEqual(snapshot['summary']['high_risk'], 1)
        self.assertEqual(snapshot['asset_status_distribution']['Retired'], 1)
        self.assertEqual(snapshot['scan_status']['Failed'], 1)
        self.assertTrue(snapshot['priority_assets'])
        self.assertTrue(snapshot['recommendations'])

