import asyncio

from sqlalchemy.exc import OperationalError
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.orm import sessionmaker, declarative_base
from src.core.config import settings

engine = create_async_engine(settings.DATABASE_URL, echo=False, pool_pre_ping=True)
async_session = sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)
Base = declarative_base()


async def _run_additive_schema_updates(connection) -> None:
    await connection.exec_driver_sql(
        """
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS device_type VARCHAR(50)
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS location VARCHAR(255)
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS tags TEXT
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS notes TEXT
        """
    )
    await connection.exec_driver_sql(
        """
        UPDATE assets
        SET device_type = 'Computer'
        WHERE device_type IS NULL OR device_type = ''
        """
    )


async def init_db() -> None:
    from src.models.asset import Asset
    from src.models.scan import AuditLog, ScanTask
    from src.models.user import User
    from src.models.vulnerability import Finding, Vulnerability

    _ = (Asset, AuditLog, ScanTask, User, Finding, Vulnerability)

    attempts = 12
    delay_seconds = 5

    for attempt in range(1, attempts + 1):
        try:
            async with engine.begin() as connection:
                await connection.run_sync(Base.metadata.create_all)
                await _run_additive_schema_updates(connection)
            return
        except OperationalError:
            if attempt == attempts:
                raise
            await asyncio.sleep(delay_seconds)
