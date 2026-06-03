import enum

from sqlalchemy import Boolean, Column, DateTime, ForeignKey, Integer, String, Text
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func

from src.models.database import Base


class CredentialKind(str, enum.Enum):
    WINDOWS_PASSWORD = 'WindowsPassword'
    LINUX_SSH_PASSWORD = 'LinuxSSHPassword'
    LINUX_SSH_KEY = 'LinuxSSHKey'
    SNMP_V2C = 'SNMPv2c'


class Credential(Base):
    __tablename__ = 'credentials'

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(120), nullable=False)
    kind = Column(String(50), nullable=False)
    owner_id = Column(Integer, ForeignKey('users.id'), nullable=False)
    username = Column(String(120), nullable=True)
    domain = Column(String(120), nullable=True)
    port = Column(Integer, nullable=True)
    secret_ciphertext = Column(Text, nullable=True)
    secondary_secret_ciphertext = Column(Text, nullable=True)
    notes = Column(Text, nullable=True)
    is_active = Column(Boolean, nullable=False, default=True)
    last_used_at = Column(DateTime(timezone=True), nullable=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    assets = relationship('Asset', back_populates='default_credential', foreign_keys='Asset.default_credential_id')
    scan_tasks = relationship('ScanTask', back_populates='credential')
