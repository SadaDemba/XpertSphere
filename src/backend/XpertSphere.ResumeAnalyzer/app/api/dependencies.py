from fastapi import Depends
from app.services.cv_service import CVService
from app.infrastructure.extractors.pdf_extractor import PDFExtractor
from app.infrastructure.analyzers.openai_analyzer import OpenAIAnalyzer
from app.core.security import verify_api_key


def get_cv_service() -> CVService:
    """
    Dependency for CV service

    Returns:
        Configured CV service
    """
    extractors = [PDFExtractor()]
    analyzer = OpenAIAnalyzer()

    return CVService(extractors=extractors, analyzer=analyzer)


def get_api_key_dependency():
    """
    Dependency for API key verification

    Returns:
        API key verification dependency
    """
    return Depends(verify_api_key)
