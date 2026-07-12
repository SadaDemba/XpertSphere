from app.services import CVService
from app.infrastructure.extractors import PDFExtractor
from app.infrastructure.analyzers import GroqAnalyzer, OpenAIAnalyzer
from app.core import settings


def get_cv_service() -> CVService:
    """
    Dependency for CV service

    Returns:
        Configured CV service
    """
    extractors = [PDFExtractor()]
    analyzer = GroqAnalyzer() if settings.LLM_PROVIDER == "groq" else OpenAIAnalyzer()

    return CVService(extractors=extractors, analyzer=analyzer)
