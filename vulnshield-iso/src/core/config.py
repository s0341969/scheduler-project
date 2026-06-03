import os
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    PROJECT_NAME: str = 'VulnShield-ISO'
    API_V1_STR: str = '/api/v1'
    SECRET_KEY: str = os.getenv('SECRET_KEY', 'super-secret-iso27001-key-change-in-prod')
    ALGORITHM: str = 'HS256'
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 60
    DEFAULT_ADMIN_USERNAME: str = os.getenv('DEFAULT_ADMIN_USERNAME', 'admin')
    DEFAULT_ADMIN_PASSWORD: str = os.getenv('DEFAULT_ADMIN_PASSWORD', 'ChangeMe123!')
    DEFAULT_ADMIN_ROLE: str = os.getenv('DEFAULT_ADMIN_ROLE', 'Admin')
    
    # Database
    POSTGRES_USER: str = os.getenv('POSTGRES_USER', 'postgres')
    POSTGRES_PASSWORD: str = os.getenv('POSTGRES_PASSWORD', 'password')
    POSTGRES_SERVER: str = os.getenv('POSTGRES_SERVER', 'db')
    POSTGRES_PORT: int = 5432
    POSTGRES_DB: str = os.getenv('POSTGRES_DB', 'vulnshield')
    
    DATABASE_URL: str = f'postgresql+asyncpg://{POSTGRES_USER}:{POSTGRES_PASSWORD}@{POSTGRES_SERVER}:{POSTGRES_PORT}/{POSTGRES_DB}'
    
    # Redis / Celery
    REDIS_URL: str = os.getenv('REDIS_URL', 'redis://redis:6379/0')
    
    class Config:
        env_file = '.env'
        case_sensitive = True

settings = Settings()
