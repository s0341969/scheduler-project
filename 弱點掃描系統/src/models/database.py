from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.orm import sessionmaker, declarative_base
from src.core.config import settings

engine = create_async_engine(settings.DATABASE_URL, echo=False, pool_pre_ping=True)
async_session = sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)
Base = declarative_base()


async def init_db() -> None:
    from src.models.asset import Asset
    from src.models.scan import AuditLog, ScanTask
    from src.models.user import User
    from src.models.vulnerability import Finding, Vulnerability

    _ = (Asset, AuditLog, ScanTask, User, Finding, Vulnerability)

    async with engine.begin() as connection:
        await connection.run_sync(Base.metadata.create_all)
