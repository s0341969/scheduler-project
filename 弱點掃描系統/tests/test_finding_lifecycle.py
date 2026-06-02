import unittest

from src.models.user import UserRole
from src.models.vulnerability import FindingStatus
from src.services.finding_lifecycle import validate_status_transition


class FindingLifecycleTests(unittest.TestCase):
    def test_fixed_to_verified_requires_auditor(self):
        with self.assertRaises(PermissionError):
            validate_status_transition(FindingStatus.FIXED, FindingStatus.VERIFIED, UserRole.ANALYST)

        validate_status_transition(FindingStatus.FIXED, FindingStatus.VERIFIED, UserRole.AUDITOR)

    def test_open_to_fixed_is_rejected(self):
        with self.assertRaises(ValueError):
            validate_status_transition(FindingStatus.OPEN, FindingStatus.FIXED, UserRole.ADMIN)


if __name__ == '__main__':
    unittest.main()
