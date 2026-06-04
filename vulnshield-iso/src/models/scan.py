from sqlalchemy import Boolean, Column, Integer, String, Enum, DateTime, ForeignKey, JSON, Text
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from src.models.database import Base
import enum

class ScanStatus(enum.Enum):
    PENDING = 'Pending'
    RUNNING = 'Running'
    COMPLETED = 'Completed'
    FAILED = 'Failed'


class ScheduleCadence(enum.Enum):
    DAILY = 'Daily'
    WEEKLY = 'Weekly'
    CRON = 'Cron'


class ScanTask(Base):
    __tablename__ = 'scan_tasks'

    id = Column(Integer, primary_key=True, index=True)
    asset_id = Column(Integer, ForeignKey('assets.id'), nullable=False)
    scan_profile = Column(String, default='standard')
    device_template = Column(String(50), nullable=True)
    credential_id = Column(Integer, ForeignKey('credentials.id'), nullable=True)
    schedule_id = Column(Integer, ForeignKey('scan_schedules.id'), nullable=True)
    status = Column(Enum(ScanStatus), default=ScanStatus.PENDING)
    started_at = Column(DateTime(timezone=True), nullable=True)
    finished_at = Column(DateTime(timezone=True), nullable=True)
    raw_output_path = Column(String, nullable=True)
    scan_summary = Column(JSON, nullable=True)
    scan_config = Column(JSON, nullable=True)
    error_message = Column(Text, nullable=True)

    asset = relationship('Asset', back_populates='scan_tasks')
    credential = relationship('Credential', back_populates='scan_tasks')
    schedule = relationship('ScanSchedule', back_populates='scan_tasks', foreign_keys=[schedule_id])


class ScanSchedule(Base):
    __tablename__ = 'scan_schedules'

    id = Column(Integer, primary_key=True, index=True)
    asset_id = Column(Integer, ForeignKey('assets.id'), nullable=False)
    name = Column(String(120), nullable=False)
    cadence = Column(String(20), nullable=False, default=ScheduleCadence.DAILY.value)
    timezone = Column(String(64), nullable=False, default='Asia/Taipei')
    weekdays = Column(String(32), nullable=True)
    run_hour = Column(Integer, nullable=True)
    run_minute = Column(Integer, nullable=True)
    cron_expr = Column(String(120), nullable=True)
    scan_profile = Column(String(50), nullable=False, default='standard')
    device_template = Column(String(50), nullable=True)
    credential_id = Column(Integer, ForeignKey('credentials.id'), nullable=True)
    is_active = Column(Boolean, nullable=False, default=True)
    next_run_at = Column(DateTime(timezone=True), nullable=True)
    last_run_at = Column(DateTime(timezone=True), nullable=True)
    last_task_id = Column(Integer, ForeignKey('scan_tasks.id'), nullable=True)
    last_error = Column(Text, nullable=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    asset = relationship('Asset', foreign_keys=[asset_id])
    credential = relationship('Credential', foreign_keys=[credential_id])
    last_task = relationship('ScanTask', foreign_keys=[last_task_id])
    scan_tasks = relationship('ScanTask', back_populates='schedule', foreign_keys='ScanTask.schedule_id')

class AuditLog(Base):
    __tablename__ = 'audit_logs'

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey('users.id'), nullable=False)
    action = Column(String, nullable=False)
    entity_type = Column(String, nullable=False)
    entity_id = Column(Integer, nullable=False)
    payload = Column(JSON)
    timestamp = Column(DateTime(timezone=True), server_default=func.now())
