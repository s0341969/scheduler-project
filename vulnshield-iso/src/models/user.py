from sqlalchemy import Column, Integer, String, DateTime, Enum
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from src.models.database import Base
import enum

class UserRole(enum.Enum):
    ADMIN = 'Admin'
    ANALYST = 'Analyst'
    AUDITOR = 'Auditor'

class User(Base):
    __tablename__ = 'users'

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String, unique=True, index=True, nullable=False)
    hashed_password = Column(String, nullable=False)
    role = Column(Enum(UserRole), default=UserRole.ANALYST)
    is_active = Column(Integer, default=1) # Using int for boolean compat
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    assets = relationship('Asset', backref='owner')
    audit_logs = relationship('AuditLog', backref='user')
