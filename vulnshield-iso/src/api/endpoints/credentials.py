from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from src.api.deps import get_db, require_roles
from src.models.credential import Credential
from src.models.user import User, UserRole
from src.schemas.credential import (
    CredentialCreate,
    CredentialKindDescriptor,
    CredentialResponse,
    CredentialUpdate,
)
from src.services.credential_service import (
    apply_credential_secrets,
    credential_to_response,
    get_credential_kind_definition,
    list_credential_kinds,
    validate_credential_payload,
)


router = APIRouter(prefix='/credentials', tags=['credentials'])


async def get_accessible_credential(credential_id: int, db: AsyncSession, current_user: User) -> Credential:
    credential = await db.get(Credential, credential_id)
    if credential is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail='找不到 credential')

    if current_user.role == UserRole.ANALYST and credential.owner_id != current_user.id:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail='您無法操作他人的 credential')

    return credential


@router.get('/kinds', response_model=list[CredentialKindDescriptor])
async def get_credential_kinds(
    _: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    return list_credential_kinds()


@router.get('', response_model=list[CredentialResponse])
async def list_credentials(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    statement = select(Credential).order_by(Credential.id.asc())
    if current_user.role == UserRole.ANALYST:
        statement = statement.where(Credential.owner_id == current_user.id)

    result = await db.execute(statement)
    return [credential_to_response(credential) for credential in result.scalars().all()]


@router.post('', response_model=CredentialResponse, status_code=status.HTTP_201_CREATED)
async def create_credential(
    payload: CredentialCreate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    try:
        get_credential_kind_definition(payload.kind)
        validate_credential_payload(
            kind=payload.kind,
            username=payload.username,
            primary_secret=payload.primary_secret,
            secondary_secret=payload.secondary_secret,
        )
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc

    credential = Credential(
        name=payload.name,
        kind=payload.kind,
        owner_id=current_user.id,
        username=payload.username,
        domain=payload.domain,
        port=payload.port or get_credential_kind_definition(payload.kind).default_port,
        notes=payload.notes,
        is_active=payload.is_active,
    )
    apply_credential_secrets(
        credential,
        primary_secret=payload.primary_secret,
        secondary_secret=payload.secondary_secret,
    )
    db.add(credential)
    await db.commit()
    await db.refresh(credential)
    return credential_to_response(credential)


@router.get('/{credential_id}', response_model=CredentialResponse)
async def get_credential(
    credential_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    credential = await get_accessible_credential(credential_id, db, current_user)
    return credential_to_response(credential)


@router.patch('/{credential_id}', response_model=CredentialResponse)
async def update_credential(
    credential_id: int,
    payload: CredentialUpdate,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    credential = await get_accessible_credential(credential_id, db, current_user)

    values = payload.model_dump(exclude_unset=True)
    for field in ('name', 'username', 'domain', 'port', 'notes', 'is_active'):
        if field in values:
            setattr(credential, field, values[field])

    if 'primary_secret' in values or 'secondary_secret' in values:
        try:
            validate_credential_payload(
                kind=credential.kind,
                username=values.get('username', credential.username),
                primary_secret=values.get('primary_secret') if 'primary_secret' in values else 'keep',
                secondary_secret=values.get('secondary_secret') if 'secondary_secret' in values else 'keep',
            )
        except ValueError as exc:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc
        apply_credential_secrets(
            credential,
            primary_secret=values.get('primary_secret') if 'primary_secret' in values else None,
            secondary_secret=values.get('secondary_secret') if 'secondary_secret' in values else None,
        )

    await db.commit()
    await db.refresh(credential)
    return credential_to_response(credential)
