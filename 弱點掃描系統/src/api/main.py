from contextlib import asynccontextmanager

from fastapi import Depends, FastAPI
from sqlalchemy import select

from src.api.deps import require_roles
from src.api.endpoints.assets import router as assets_router
from src.api.endpoints.auth import router as auth_router
from src.api.endpoints.findings import router as findings_router
from src.api.endpoints.scans import router as scans_router
from src.core.config import settings
from src.core.security import get_password_hash
from src.models.database import async_session, init_db
from src.models.user import User, UserRole
from src.models.vulnerability import Finding


@asynccontextmanager
async def lifespan(_: FastAPI):
    await init_db()
    async with async_session() as session:
        result = await session.execute(select(User).where(User.username == settings.DEFAULT_ADMIN_USERNAME))
        admin = result.scalar_one_or_none()
        if admin is None:
            session.add(
                User(
                    username=settings.DEFAULT_ADMIN_USERNAME,
                    hashed_password=get_password_hash(settings.DEFAULT_ADMIN_PASSWORD),
                    role=UserRole(settings.DEFAULT_ADMIN_ROLE),
                    is_active=1,
                )
            )
            await session.commit()
    yield


app = FastAPI(title=settings.PROJECT_NAME, lifespan=lifespan)
app.include_router(auth_router)
app.include_router(assets_router)
app.include_router(scans_router)
app.include_router(findings_router)


@app.get('/healthz', tags=['health'])
async def healthz():
    return {'status': 'ok'}


@app.get('/reports/iso27001', tags=['reports'])
async def get_iso_report(
    _: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    async with async_session() as db:
        result = await db.execute(select(Finding))
        findings = result.scalars().all()

        high_risk = len([f for f in findings if f.risk_score is not None and f.risk_score >= 15.0])
        med_risk = len([f for f in findings if f.risk_score is not None and 5.0 <= f.risk_score < 15.0])
        low_risk = len([f for f in findings if f.risk_score is not None and f.risk_score < 5.0])

        return {
            'summary': {
                'total_findings': len(findings),
                'high_risk': high_risk,
                'medium_risk': med_risk,
                'low_risk': low_risk,
            },
            'compliance_status': 'In Progress' if high_risk > 0 else 'Compliant'
        }

