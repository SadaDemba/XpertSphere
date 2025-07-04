from fastapi import APIRouter
from app.api.endpoints import resume

router = APIRouter()

# Include all API routers
router.include_router(resume.router, prefix="/cv", tags=["CV Extraction"])
