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
from src.api.endpoints.findings import router as findings_router
from src.api.endpoints.scans import router as scans_router
from src.core.config import settings
from src.core.security import get_password_hash
from src.models.database import async_session, init_db
from src.models.scan import ScanStatus, ScanTask
from src.models.user import User, UserRole
from src.models.vulnerability import Finding

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
        result = await db.execute(select(Finding))
        findings = result.scalars().all()
        scan_result = await db.execute(select(ScanTask))
        scans = scan_result.scalars().all()

        high_risk = len([f for f in findings if f.risk_score is not None and f.risk_score >= 15.0])
        med_risk = len([f for f in findings if f.risk_score is not None and 5.0 <= f.risk_score < 15.0])
        low_risk = len([f for f in findings if f.risk_score is not None and f.risk_score < 5.0])

        scan_layers = {
            'services': 0,
            'vulnerabilities': 0,
            'misconfigurations': 0,
            'certificate_risks': 0,
            'exposures': 0,
            'informational': 0,
        }
        scan_status = {
            ScanStatus.PENDING.value: 0,
            ScanStatus.RUNNING.value: 0,
            ScanStatus.COMPLETED.value: 0,
            ScanStatus.FAILED.value: 0,
        }
        profile_distribution: dict[str, int] = {}

        for scan in scans:
            scan_status[scan.status.value] = scan_status.get(scan.status.value, 0) + 1
            profile_distribution[scan.scan_profile] = profile_distribution.get(scan.scan_profile, 0) + 1

            summary = scan.scan_summary or {}
            scan_layers['services'] += int(summary.get('service_count', 0) or 0)
            scan_layers['vulnerabilities'] += int(summary.get('vulnerability_count', 0) or 0)
            scan_layers['misconfigurations'] += int(summary.get('misconfiguration_count', 0) or 0)
            scan_layers['certificate_risks'] += int(summary.get('certificate_risk_count', 0) or 0)
            scan_layers['exposures'] += int(summary.get('exposure_count', 0) or 0)
            scan_layers['informational'] += int(summary.get('informational_count', 0) or 0)

        return {
            'summary': {
                'total_findings': len(findings),
                'high_risk': high_risk,
                'medium_risk': med_risk,
                'low_risk': low_risk,
            },
            'scan_layers': scan_layers,
            'scan_status': scan_status,
            'profile_distribution': profile_distribution,
            'compliance_status': 'In Progress' if high_risk > 0 else 'Compliant'
        }

