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
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS default_scan_profile VARCHAR(50)
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS template_key VARCHAR(50)
        """
    )
    await connection.exec_driver_sql(
        """
        CREATE TABLE IF NOT EXISTS credentials (
            id SERIAL PRIMARY KEY,
            name VARCHAR(120) NOT NULL,
            kind VARCHAR(50) NOT NULL,
            owner_id INTEGER NOT NULL REFERENCES users(id),
            username VARCHAR(120),
            domain VARCHAR(120),
            port INTEGER,
            secret_ciphertext TEXT,
            secondary_secret_ciphertext TEXT,
            notes TEXT,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            last_used_at TIMESTAMPTZ,
            created_at TIMESTAMPTZ DEFAULT NOW(),
            updated_at TIMESTAMPTZ
        )
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE assets
        ADD COLUMN IF NOT EXISTS default_credential_id INTEGER
        """
    )
    await connection.exec_driver_sql(
        """
        UPDATE assets
        SET device_type = 'Computer'
        WHERE device_type IS NULL OR device_type = ''
        """
    )
    await connection.exec_driver_sql(
        """
        UPDATE assets
        SET default_scan_profile = 'standard'
        WHERE default_scan_profile IS NULL OR default_scan_profile = '' OR default_scan_profile = 'full'
        """
    )
    await connection.exec_driver_sql(
        """
        UPDATE assets
        SET template_key = CASE
            WHEN device_type = 'Firewall' THEN 'firewall'
            WHEN device_type IN ('Switch', 'Router', 'NetworkDevice') THEN 'switch'
            WHEN device_type = 'NAS' THEN 'nas'
            ELSE 'generic'
        END
        WHERE template_key IS NULL OR template_key = ''
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE scan_tasks
        ADD COLUMN IF NOT EXISTS scan_summary JSONB
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE scan_tasks
        ADD COLUMN IF NOT EXISTS device_template VARCHAR(50)
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE scan_tasks
        ADD COLUMN IF NOT EXISTS credential_id INTEGER
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE scan_tasks
        ADD COLUMN IF NOT EXISTS scan_config JSONB
        """
    )
    await connection.exec_driver_sql(
        """
        CREATE TABLE IF NOT EXISTS scan_schedules (
            id SERIAL PRIMARY KEY,
            asset_id INTEGER NOT NULL REFERENCES assets(id),
            name VARCHAR(120) NOT NULL,
            cadence VARCHAR(20) NOT NULL DEFAULT 'Daily',
            timezone VARCHAR(64) NOT NULL DEFAULT 'Asia/Taipei',
            weekdays VARCHAR(32),
            run_hour INTEGER,
            run_minute INTEGER,
            cron_expr VARCHAR(120),
            scan_profile VARCHAR(50) NOT NULL DEFAULT 'standard',
            device_template VARCHAR(50),
            credential_id INTEGER REFERENCES credentials(id),
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            next_run_at TIMESTAMPTZ,
            last_run_at TIMESTAMPTZ,
            last_task_id INTEGER,
            last_error TEXT,
            created_at TIMESTAMPTZ DEFAULT NOW(),
            updated_at TIMESTAMPTZ
        )
        """
    )
    await connection.exec_driver_sql(
        """
        ALTER TABLE scan_tasks
        ADD COLUMN IF NOT EXISTS schedule_id INTEGER
        """
    )
    await connection.exec_driver_sql(
        """
        UPDATE scan_tasks
        SET scan_profile = 'standard'
        WHERE scan_profile IS NULL OR scan_profile = '' OR scan_profile = 'full'
        """
    )


async def init_db() -> None:
    from src.models.asset import Asset
    from src.models.credential import Credential
    from src.models.scan import AuditLog, ScanSchedule, ScanTask
    from src.models.user import User
    from src.models.vulnerability import Finding, Vulnerability

    _ = (Asset, Credential, AuditLog, ScanSchedule, ScanTask, User, Finding, Vulnerability)

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
