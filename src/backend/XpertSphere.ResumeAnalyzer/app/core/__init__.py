from .config import settings
import logging
from .exceptions import AnalysisError, BaseApplicationError, ExtractionError, ValidationError

__all__ = ["settings", "AnalysisError", "BaseApplicationError", "ExtractionError", "ValidationError"]


def __init__(self, **kwargs):
    super().__init__(**kwargs)
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )