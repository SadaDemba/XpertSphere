from fastapi import APIRouter, HTTPException, status
from fastapi.responses import JSONResponse
from datetime import datetime, timezone
from typing import Dict, Any
import sys
import os
import asyncio
from app.core.config import settings
from app.utils import get_llm

router = APIRouter()


async def check_azure_openai() -> Dict[str, Any]:
    """Check Azure OpenAI connectivity"""
    try:
        if not settings.AZURE_OPENAI_ENDPOINT or not settings.AZURE_OPENAI_API_KEY:
            return {
                "status": "error",
                "message": "Azure OpenAI credentials not configured"
            }
        
        # Simple connection test (no actual request to save tokens)
        client = get_llm()
        return {
            "status": "ok",
            "endpoint": settings.AZURE_OPENAI_ENDPOINT[:30] + "..." if settings.AZURE_OPENAI_ENDPOINT else "Not set",
            "deployment": settings.current_deployment,
            "api_version": settings.AZURE_OPENAI_API_VERSION
        }
    except Exception as e:
        return {
            "status": "error", 
            "message": str(e)
        }


def get_system_info() -> Dict[str, Any]:
    """Get system information"""
    try:
        # System information
        memory = psutil.virtual_memory()
        disk = psutil.disk_usage('/')
        
        return {
            "cpu_percent": psutil.cpu_percent(interval=1),
            "memory": {
                "total_gb": round(memory.total / (1024**3), 2),
                "available_gb": round(memory.available / (1024**3), 2),
                "percent_used": memory.percent
            },
            "disk": {
                "total_gb": round(disk.total / (1024**3), 2),
                "free_gb": round(disk.free / (1024**3), 2),
                "percent_used": round((disk.used / disk.total) * 100, 1)
            }
        }
    except Exception as e:
        return {"error": str(e)}


@router.get("/health")
async def health_check():
    """
    Basic health check - always fast for Azure Container Apps health probes
    """
    return {
        "status": "healthy",
        "service": "XpertSphere Resume Analyzer",
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "version": "1.0.0",
        "environment": settings.ENVIRONMENT
    }


@router.get("/health/detailed")
async def detailed_health_check():
    """
    Detailed health check with dependency verification
    """
    start_time = datetime.now(timezone.utc)
    
    # Parallel checks
    azure_openai_task = asyncio.create_task(check_azure_openai())
    
    # Basic information
    health_data = {
        "status": "healthy",
        "service": "XpertSphere Resume Analyzer", 
        "timestamp": start_time.isoformat(),
        "version": "1.0.0",
        "environment": settings.ENVIRONMENT,
        "python_version": f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}",
        "uptime": "runtime info not available",  # Could be improved with global variable
        "system": get_system_info(),
        "configuration": {
            "key_vault_url": settings.KEY_VAULT_URL,
            "azure_client_id": "***SET***" if settings.AZURE_CLIENT_ID else "NOT SET",
            "max_file_size_mb": settings.MAX_FILE_SIZE_MB
        },
        "dependencies": {
            "azure_openai": await azure_openai_task
        }
    }
    
    # Determine overall status
    overall_status = "healthy"
    if health_data["dependencies"]["azure_openai"]["status"] == "error":
        overall_status = "degraded"
    
    health_data["status"] = overall_status
    health_data["response_time_ms"] = int((datetime.now(timezone.utc) - start_time).total_seconds() * 1000)
    
    # Return appropriate HTTP status code
    status_code = status.HTTP_200_OK if overall_status == "healthy" else status.HTTP_503_SERVICE_UNAVAILABLE
    
    return JSONResponse(
        content=health_data,
        status_code=status_code
    )


@router.get("/health/readiness")
async def readiness_check():
    """
    Readiness check for Azure Container Apps - verifies app can handle requests
    """
    try:
        # Minimal check: Azure OpenAI configuration
        if not settings.AZURE_OPENAI_ENDPOINT or not settings.AZURE_OPENAI_API_KEY:
            raise HTTPException(
                status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
                detail="Service not ready: Azure OpenAI not configured"
            )
        
        return {
            "status": "ready",
            "service": "XpertSphere Resume Analyzer",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "checks": {
                "azure_openai_config": "ok",
                "environment": settings.ENVIRONMENT
            }
        }
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=f"Service not ready: {str(e)}"
        )


@router.get("/health/liveness")
async def liveness_check():
    """
    Liveness check for Azure Container Apps - verifies app is alive
    """
    return {
        "status": "alive",
        "service": "XpertSphere Resume Analyzer",
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "pid": os.getpid()
    }