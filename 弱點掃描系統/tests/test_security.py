import unittest

from src.core.security import create_access_token, decode_access_token, get_password_hash, verify_password


class SecurityTests(unittest.TestCase):
    def test_password_hash_roundtrip(self):
        password = 'StrongPass!123'
        hashed = get_password_hash(password)

        self.assertNotEqual(password, hashed)
        self.assertTrue(verify_password(password, hashed))
        self.assertFalse(verify_password('wrong-password', hashed))

    def test_access_token_roundtrip(self):
        token = create_access_token('alice')
        subject = decode_access_token(token)

        self.assertEqual(subject, 'alice')


if __name__ == '__main__':
    unittest.main()
