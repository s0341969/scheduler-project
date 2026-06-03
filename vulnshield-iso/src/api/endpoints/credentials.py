from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from src.api.deps import get_db, require_roles
from src.models.asset import Asset
from src.models.credential import Credential
from src.models.scan import AuditLog, ScanStatus, ScanTask
from src.models.user import User, UserRole
from src.schemas.audit import AuditLogResponse
from src.schemas.credential import (
    CredentialCreate,
    CredentialKindDescriptor,
    CredentialResponse,
    CredentialUpdate,
)
from src.services.audit_service import add_audit_log
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
    await db.flush()
    add_audit_log(
        db,
        user=current_user,
        action='CREATE_CREDENTIAL',
        entity_type='Credential',
        entity_id=credential.id,
        payload={'kind': credential.kind, 'name': credential.name},
    )
    await db.commit()
    await db.refresh(credential)
    return credential_to_response(credential)


@router.get('/{credential_id}/audit', response_model=list[AuditLogResponse])
async def list_credential_audit_logs(
    credential_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    credential = await get_accessible_credential(credential_id, db, current_user)
    result = await db.execute(
        select(AuditLog)
        .where(AuditLog.entity_type == 'Credential', AuditLog.entity_id == credential.id)
        .order_by(AuditLog.id.desc())
        .limit(20)
    )
    return result.scalars().all()


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
    old_values = {
        'name': credential.name,
        'username': credential.username,
        'domain': credential.domain,
        'port': credential.port,
        'notes': credential.notes,
        'is_active': credential.is_active,
    }

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

    changed_fields = {
        field: {'old': old_values[field], 'new': getattr(credential, field)}
        for field in old_values
        if old_values[field] != getattr(credential, field)
    }
    if 'primary_secret' in values:
        changed_fields['primary_secret'] = {'old': 'hidden', 'new': 'updated'}
    if 'secondary_secret' in values:
        changed_fields['secondary_secret'] = {'old': 'hidden', 'new': 'updated'}

    if changed_fields:
        add_audit_log(
            db,
            user=current_user,
            action='UPDATE_CREDENTIAL',
            entity_type='Credential',
            entity_id=credential.id,
            payload=changed_fields,
        )

    await db.commit()
    await db.refresh(credential)
    return credential_to_response(credential)


@router.delete('/{credential_id}', status_code=status.HTTP_204_NO_CONTENT)
async def delete_credential(
    credential_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST)),
):
    credential = await get_accessible_credential(credential_id, db, current_user)

    bound_assets = await db.scalar(
        select(func.count(Asset.id)).where(Asset.default_credential_id == credential.id)
    )
    active_scans = await db.scalar(
        select(func.count(ScanTask.id)).where(
            ScanTask.credential_id == credential.id,
            ScanTask.status.in_([ScanStatus.PENDING, ScanStatus.RUNNING]),
        )
    )

    if bound_assets:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail='此 credential 仍綁定設備，請先移除設備上的預設 credential 再刪除',
        )
    if active_scans:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail='此 credential 仍被執行中的掃描任務使用，請待任務完成後再刪除',
        )

    add_audit_log(
        db,
        user=current_user,
        action='DELETE_CREDENTIAL',
        entity_type='Credential',
        entity_id=credential.id,
        payload={'name': credential.name, 'kind': credential.kind},
    )
    await db.delete(credential)
    await db.commit()
