from abc import ABC, abstractmethod
from typing import Any, Dict, Optional
from app.domain.models.resume import CVModel


class TextAnalyzer(ABC):
    """
    Interface for text analyzers that extract structured information
    """

    @abstractmethod
    async def analyze(
        self, text: str, options: Optional[Dict[str, Any]] = None
    ) -> CVModel:
        """
        Analyze text and extract structured information

        Args:
            text: Text to analyze
            options: Optional parameters for the analyzer

        Returns:
            Structured CV model

        Raises:
            AnalysisError: If analysis fails
        """
        pass
