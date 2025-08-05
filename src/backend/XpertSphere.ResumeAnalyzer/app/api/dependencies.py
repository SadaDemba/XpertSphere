from app.services import CVService
from app.infrastructure.extractors import PDFExtractor
from app.infrastructure.analyzers import OpenAIAnalyzer


def get_cv_service() -> CVService:
    """
    Dependency for CV service

    Returns:
        Configured CV service
    """
    extractors = [PDFExtractor()]
    analyzer = OpenAIAnalyzer()

    return CVService(extractors=extractors, analyzer=analyzer)
