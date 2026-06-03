from sqlalchemy import Column, Integer, String, Enum, DateTime, ForeignKey, JSON, Text
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from src.models.database import Base
import enum

class ScanStatus(enum.Enum):
    PENDING = 'Pending'
    RUNNING = 'Running'
    COMPLETED = 'Completed'
    FAILED = 'Failed'

class ScanTask(Base):
    __tablename__ = 'scan_tasks'

    id = Column(Integer, primary_key=True, index=True)
    asset_id = Column(Integer, ForeignKey('assets.id'), nullable=False)
    scan_profile = Column(String, default='standard')
    device_template = Column(String(50), nullable=True)
    credential_id = Column(Integer, ForeignKey('credentials.id'), nullable=True)
    status = Column(Enum(ScanStatus), default=ScanStatus.PENDING)
    started_at = Column(DateTime(timezone=True), nullable=True)
    finished_at = Column(DateTime(timezone=True), nullable=True)
    raw_output_path = Column(String, nullable=True)
    scan_summary = Column(JSON, nullable=True)
    scan_config = Column(JSON, nullable=True)
    error_message = Column(Text, nullable=True)

    asset = relationship('Asset', back_populates='scan_tasks')
    credential = relationship('Credential', back_populates='scan_tasks')

class AuditLog(Base):
    __tablename__ = 'audit_logs'

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey('users.id'), nullable=False)
    action = Column(String, nullable=False)
    entity_type = Column(String, nullable=False)
    entity_id = Column(Integer, nullable=False)
    payload = Column(JSON)
    timestamp = Column(DateTime(timezone=True), server_default=func.now())
