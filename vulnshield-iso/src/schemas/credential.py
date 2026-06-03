from datetime import datetime
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class CredentialKindDescriptor(BaseModel):
    key: str
    label: str
    description: str
    requires_username: bool
    requires_primary_secret: bool
    requires_secondary_secret: bool
    supports_domain: bool
    default_port: Optional[int] = None


class CredentialBase(BaseModel):
    name: str = Field(..., example='台北機房 Windows Domain Admin')
    kind: str = Field(..., example='WindowsPassword')
    username: Optional[str] = Field(default=None, example='svc_scanner')
    domain: Optional[str] = Field(default=None, example='corp')
    port: Optional[int] = Field(default=None, example=5985)
    notes: Optional[str] = Field(default=None, example='僅供維護窗口執行認證掃描')
    is_active: bool = True


class CredentialCreate(CredentialBase):
    primary_secret: Optional[str] = Field(default=None, example='StrongPassword123!')
    secondary_secret: Optional[str] = Field(default=None, example='SSH KEY 或 passphrase')


class CredentialUpdate(BaseModel):
    name: Optional[str] = None
    username: Optional[str] = None
    domain: Optional[str] = None
    port: Optional[int] = None
    notes: Optional[str] = None
    is_active: Optional[bool] = None
    primary_secret: Optional[str] = None
    secondary_secret: Optional[str] = None


class CredentialResponse(CredentialBase):
    id: int
    owner_id: int
    kind_label: str
    has_primary_secret: bool
    has_secondary_secret: bool
    last_used_at: Optional[datetime] = None
    created_at: datetime
    updated_at: Optional[datetime] = None

    model_config = ConfigDict(from_attributes=True)
