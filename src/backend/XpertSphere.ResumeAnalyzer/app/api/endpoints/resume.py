from fastapi import APIRouter, File, UploadFile, HTTPException, Depends, Query
from app.services.cv_service import CVService
from app.api.dependencies import get_cv_service, get_api_key_dependency
from app.domain.models.resume import CVModel
from app.core.config import settings
from typing import Dict, Any, Optional

router = APIRouter()


@router.post("/extract/", response_model=Dict[str, Any])
async def extract_data_from_cv(
    file: UploadFile = File(...),
    cv_service: CVService = Depends(get_cv_service),
    include_raw_text: bool = Query(
        False, description="Include extracted raw text in response"
    ),
    _: str = get_api_key_dependency(),
):
    """
    Extract structured information from a CV

    Args:
        file: CV file (PDF supported)
        cv_service: CV processing service
        include_raw_text: Whether to include raw extracted text in response

    Returns:
        Structured CV information
    """
    # Check file size
    max_size_bytes = settings.MAX_FILE_SIZE_MB * 1024 * 1024
    file_size = 0
    contents = await file.read()
    file_size = len(contents)
    await file.seek(0)

    if file_size > max_size_bytes:
        raise HTTPException(
            status_code=400,
            detail=f"File too large. Maximum allowed size is {settings.MAX_FILE_SIZE_MB} MB.",
        )

    # Process CV
    options = {"include_raw_text": include_raw_text}
    cv_model = await cv_service.process_cv(file, options)

    # Return response
    return {"extracted_data": cv_model}
