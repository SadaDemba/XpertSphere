from typing import Dict, List, Any, Optional, Union

from fastapi import UploadFile, HTTPException
import logging
from app.domain.interfaces.document_extractor import DocumentExtractor
from app.domain.interfaces.text_analyzer import TextAnalyzer
from app.domain.models.resume import CVModel
from app.core.exceptions import ExtractionError, AnalysisError


class CVService:
    """
    Service for CV extraction and analysis
    """

    def __init__(self, extractors: List[DocumentExtractor], analyzer: TextAnalyzer):
        """
        Initialize the CV service

        Args:
            extractors: List of document extractors
            analyzer: Text analyzer for CV parsing
        """
        self.extractors = extractors
        self.analyzer = analyzer
        self.logger = logging.getLogger(self.__class__.__name__)

    async def process_cv(
        self, file: UploadFile, options: Optional[Dict[str, Any]] = None
    ) -> CVModel:
        """
        Process a CV file to extract structured information

        Args:
            file: Uploaded CV file
            options: Optional processing parameters

        Returns:
            Structured CV model

        Raises:
            HTTPException: If processing fails
        """
        # Find an appropriate extractor
        extractor = self._get_extractor(file.filename)
        if not extractor:
            raise HTTPException(
                status_code=400, detail=f"Unsupported file format: {file.filename}"
            )

        try:
            # Read file content
            content = await file.read()

            # Extract text from document
            text = await extractor.extract_text(content, file.filename)

            # Analyze text to extract structured information
            cv_model = await self.analyzer.analyze(text, options)

            return cv_model

        except ExtractionError as e:
            self.logger.error(f"Extraction error: {str(e)}")
            raise HTTPException(
                status_code=422,
                detail=f"Failed to extract text from document: {str(e)}",
            )

        except AnalysisError as e:
            self.logger.error(f"Analysis error: {str(e)}")
            raise HTTPException(
                status_code=422, detail=f"Failed to analyze CV: {str(e)}"
            )

        except Exception as e:
            self.logger.error(f"Unexpected error: {str(e)}")
            raise HTTPException(
                status_code=500, detail=f"An unexpected error occurred: {str(e)}"
            )
        finally:
            # Reset file pointer for potential reuse
            await file.seek(0)

    def _get_extractor(self, file_name: str) -> Optional[DocumentExtractor]:
        """
        Find an appropriate extractor for the given file

        Args:
            file_name: Name of the file

        Returns:
            Appropriate extractor or None if no suitable extractor is found
        """
        for extractor in self.extractors:
            if extractor.can_extract(file_name):
                return extractor
        return None
