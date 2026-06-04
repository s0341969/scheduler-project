from celery import Celery
from celery.schedules import crontab
from src.core.config import settings

celery_app = Celery(
    'vulnshield_worker',
    broker=settings.REDIS_URL,
    backend=settings.REDIS_URL
)

celery_app.conf.update(
    task_serializer='json',
    accept_content=['json'],
    result_serializer='json',
    timezone='Asia/Taipei',
    enable_utc=True,
    beat_schedule={
        'sync-scan-schedules-every-minute': {
            'task': 'tasks.sync_scan_schedules',
            'schedule': crontab(),
        }
    },
)

# Ensure task modules are imported when the worker boots so registered task names
# like `tasks.execute_scan` are available to Celery consumers.
celery_app.conf.imports = ('src.worker.tasks',)

