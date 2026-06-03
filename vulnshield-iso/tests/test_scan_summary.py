import unittest

from src.services.scan_summary import parse_nmap_services, summarize_scan_results


class ScanSummaryTests(unittest.TestCase):
    def test_parse_nmap_services_extracts_open_ports(self):
        nmap_results = [
            {
                'type': 'service_discovery',
                'output': (
                    '22/tcp open  ssh     OpenSSH 8.9p1 Ubuntu 3ubuntu0.10\n'
                    '80/tcp open  http    nginx 1.24.0\n'
                ),
            }
        ]

        services = parse_nmap_services(nmap_results)

        self.assertEqual(len(services), 2)
        self.assertEqual(services[0]['port'], '22')
        self.assertEqual(services[0]['service'], 'ssh')
        self.assertIn('OpenSSH', services[0]['product'])

    def test_summarize_scan_results_splits_vulns_and_info(self):
        summary = summarize_scan_results(
            nmap_results=[],
            nuclei_results=[
                {
                    'template-id': 'ssl-misconfig',
                    'matcher-name': 'tls',
                    'matched-at': 'https://fw.example.com',
                    'info': {
                        'name': 'TLS Weak Cipher',
                        'severity': 'medium',
                        'description': 'Weak cipher suites detected.',
                    },
                },
                {
                    'template-id': 'tech-detect',
                    'matcher-name': 'banner',
                    'matched-at': 'https://fw.example.com',
                    'info': {
                        'name': 'Technology Detection',
                        'severity': 'info',
                        'description': 'Detected management portal.',
                    },
                },
            ],
        )

        self.assertEqual(summary['vulnerability_count'], 1)
        self.assertEqual(summary['informational_count'], 1)
        self.assertEqual(summary['vulnerabilities'][0]['title'], 'TLS Weak Cipher')
        self.assertEqual(summary['informational'][0]['title'], 'Technology Detection')


if __name__ == '__main__':
    unittest.main()
