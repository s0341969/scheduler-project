from datetime import timedelta

from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import OAuth2PasswordRequestForm
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from src.api.deps import get_current_user, get_db, require_roles
from src.core.config import settings
from src.core.security import create_access_token, get_password_hash, verify_password
from src.models.user import User, UserRole
from src.schemas.user import Token, UserCreate, UserResponse


router = APIRouter(tags=['auth'])


@router.post('/token', response_model=Token)
async def login(
    form_data: OAuth2PasswordRequestForm = Depends(),
    db: AsyncSession = Depends(get_db),
):
    result = await db.execute(select(User).where(User.username == form_data.username))
    user = result.scalar_one_or_none()

    if user is None or not verify_password(form_data.password, user.hashed_password):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail='帳號或密碼錯誤',
            headers={'WWW-Authenticate': 'Bearer'},
        )

    access_token = create_access_token(
        user.username,
        expires_delta=timedelta(minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES),
    )
    return Token(access_token=access_token, token_type='bearer')


@router.post(
    '/users',
    response_model=UserResponse,
    status_code=status.HTTP_201_CREATED,
    dependencies=[Depends(require_roles(UserRole.ADMIN))],
)
async def create_user(user: UserCreate, db: AsyncSession = Depends(get_db)):
    existing_user = await db.execute(select(User).where(User.username == user.username))
    if existing_user.scalar_one_or_none() is not None:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail='使用者已存在')

    new_user = User(
        username=user.username,
        hashed_password=get_password_hash(user.password),
        role=user.role,
        is_active=1,
    )
    db.add(new_user)
    await db.commit()
    await db.refresh(new_user)
    return new_user


@router.get('/users/me', response_model=UserResponse)
async def read_current_user(current_user: User = Depends(get_current_user)):
    return current_user
