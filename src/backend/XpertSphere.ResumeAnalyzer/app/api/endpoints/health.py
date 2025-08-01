from fastapi import APIRouter
from datetime import datetime, timezone
import sys
import os

router = APIRouter()


@router.get("/health")
async def health_check():
    """
    Health check endpoint for the Resume Analyzer service
    """
    return {
        "status": "healthy",
        "service": "XpertSphere Resume Analyzer",
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "version": "1.0.0",
        "python_version": f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}",
        "environment": os.getenv("ENVIRONMENT", "development")
    }


@router.get("/readiness")
async def readiness_check():
    """
    Readiness check endpoint
    """
    try:
        return {
            "status": "ready",
            "service": "XpertSphere Resume Analyzer",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "checks": {
                "basic": "ok"
            }
        }
    except Exception as e:
        return {
            "status": "not_ready",
            "service": "XpertSphere Resume Analyzer",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "error": str(e)
        }
