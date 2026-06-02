from pydantic import BaseModel
from src.models.user import UserRole

class UserCreate(BaseModel):
    username: str
    password: str
    role: UserRole = UserRole.ANALYST

class UserResponse(BaseModel):
    id: int
    username: str
    role: UserRole
    is_active: int

    class Config:
        from_attributes = True

class Token(BaseModel):
    access_token: str
    token_type: str
