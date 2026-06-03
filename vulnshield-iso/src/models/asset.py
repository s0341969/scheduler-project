import enum

from sqlalchemy import Column, Integer, String, Enum, DateTime, ForeignKey, Text
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from src.models.database import Base


class EnvType(enum.Enum):
    PROD = 'Production'
    STAGING = 'Staging'
    DEV = 'Development'


class DeviceType(str, enum.Enum):
    COMPUTER = 'Computer'
    SERVER = 'Server'
    FIREWALL = 'Firewall'
    ROUTER = 'Router'
    SWITCH = 'Switch'
    NAS = 'NAS'
    NETWORK_DEVICE = 'NetworkDevice'
    OTHER = 'Other'


class Asset(Base):
    __tablename__ = 'assets'

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, nullable=False)
    target = Column(String, nullable=False)  # IP or Domain
    criticality = Column(Integer, nullable=False)  # 1-5
    owner_id = Column(Integer, ForeignKey('users.id'))
    env = Column(Enum(EnvType), nullable=False)
    device_type = Column(String(50), nullable=False, default=DeviceType.COMPUTER.value)
    location = Column(String(255), nullable=True)
    tags = Column(Text, nullable=True)
    notes = Column(Text, nullable=True)
    status = Column(String, default='Active')
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    findings = relationship('Finding', back_populates='asset')
    scan_tasks = relationship('ScanTask', back_populates='asset')
