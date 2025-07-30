from fastapi import APIRouter
from app.api.endpoints import resume, health

router = APIRouter()

# Include all API routers
router.include_router(health.router, tags=["Health"])
router.include_router(resume.router, tags=["CV Extraction"])
