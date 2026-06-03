import base64
import hashlib
from datetime import datetime, timedelta, timezone
from typing import Any

from cryptography.fernet import Fernet, InvalidToken
from jose import JWTError, jwt
from passlib.context import CryptContext

from src.core.config import settings


password_context = CryptContext(schemes=['pbkdf2_sha256'], deprecated='auto')
_credential_fernet: Fernet | None = None


def verify_password(plain_password: str, hashed_password: str) -> bool:
    return password_context.verify(plain_password, hashed_password)


def get_password_hash(password: str) -> str:
    return password_context.hash(password)


def create_access_token(subject: str, expires_delta: timedelta | None = None) -> str:
    expire_at = datetime.now(timezone.utc) + (
        expires_delta or timedelta(minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES)
    )
    payload: dict[str, Any] = {
        'sub': subject,
        'exp': expire_at,
    }
    return jwt.encode(payload, settings.SECRET_KEY, algorithm=settings.ALGORITHM)


def decode_access_token(token: str) -> str:
    try:
        payload = jwt.decode(token, settings.SECRET_KEY, algorithms=[settings.ALGORITHM])
    except JWTError as exc:
        raise ValueError('Invalid access token') from exc

    subject = payload.get('sub')
    if not isinstance(subject, str) or not subject:
        raise ValueError('Access token subject is missing')

    return subject


def _build_fernet() -> Fernet:
    raw_key = settings.CREDENTIAL_ENCRYPTION_KEY or settings.SECRET_KEY
    digest = hashlib.sha256(raw_key.encode('utf-8')).digest()
    fernet_key = base64.urlsafe_b64encode(digest)
    return Fernet(fernet_key)


def get_credential_fernet() -> Fernet:
    global _credential_fernet
    if _credential_fernet is None:
        _credential_fernet = _build_fernet()
    return _credential_fernet


def encrypt_credential_value(value: str | None) -> str | None:
    if value is None:
        return None
    normalized = value.strip()
    if not normalized:
        return None
    return get_credential_fernet().encrypt(normalized.encode('utf-8')).decode('utf-8')


def decrypt_credential_value(ciphertext: str | None) -> str | None:
    if ciphertext is None:
        return None
    try:
        return get_credential_fernet().decrypt(ciphertext.encode('utf-8')).decode('utf-8')
    except InvalidToken as exc:
        raise ValueError('Credential secret could not be decrypted') from exc
