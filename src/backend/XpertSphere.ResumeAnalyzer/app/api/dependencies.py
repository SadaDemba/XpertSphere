from fastapi import Depends
from app.services.cv_service import CVService
from app.infrastructure.extractors.pdf_extractor import PDFExtractor
from app.infrastructure.analyzers.openai_analyzer import OpenAIAnalyzer


def get_cv_service() -> CVService:
    """
    Dependency for CV service

    Returns:
        Configured CV service
    """
    extractors = [PDFExtractor()]
    analyzer = OpenAIAnalyzer()

    return CVService(extractors=extractors, analyzer=analyzer)
