from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import Depends, FastAPI, Request
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from sqlalchemy import select

from src.api.deps import require_roles
from src.api.endpoints.assets import router as assets_router
from src.api.endpoints.auth import router as auth_router
from src.api.endpoints.credentials import router as credentials_router
from src.api.endpoints.findings import router as findings_router
from src.api.endpoints.scans import router as scans_router
from src.core.config import settings
from src.core.security import get_password_hash
from src.models.database import async_session, init_db
from src.models.asset import Asset
from src.models.scan import ScanTask
from src.models.user import User, UserRole
from src.models.vulnerability import Finding
from src.services.reporting import build_report_snapshot

BASE_DIR = Path(__file__).resolve().parents[1]
UI_DIR = BASE_DIR / 'ui'


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
app.mount('/static', StaticFiles(directory=str(UI_DIR / 'static')), name='static')
templates = Jinja2Templates(directory=str(UI_DIR / 'templates'))
app.include_router(auth_router)
app.include_router(credentials_router)
app.include_router(assets_router)
app.include_router(scans_router)
app.include_router(findings_router)


@app.get('/', include_in_schema=False)
async def home():
    return RedirectResponse(url='/dashboard')


@app.get('/dashboard', include_in_schema=False, response_class=HTMLResponse)
async def dashboard(request: Request):
    return templates.TemplateResponse(
        request,
        'dashboard.html',
        {
            'project_name': settings.PROJECT_NAME,
        },
    )


@app.get('/healthz', tags=['health'])
async def healthz():
    return {'status': 'ok'}


@app.get('/reports/iso27001', tags=['reports'])
async def get_iso_report(
    _: User = Depends(require_roles(UserRole.ADMIN, UserRole.ANALYST, UserRole.AUDITOR)),
):
    async with async_session() as db:
        findings = (await db.execute(select(Finding))).scalars().all()
        scans = (await db.execute(select(ScanTask))).scalars().all()
        assets = (await db.execute(select(Asset))).scalars().all()
        return build_report_snapshot(assets, findings, scans)

