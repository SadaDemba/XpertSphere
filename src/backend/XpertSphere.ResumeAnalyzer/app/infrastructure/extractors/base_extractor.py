from abc import ABC
from typing import List, Set
from app.domain.interfaces.document_extractor import DocumentExtractor
from app.core.exceptions import ExtractionError
import logging


class BaseExtractor(DocumentExtractor, ABC):
    """
    Base class for document extractors
    """

    def __init__(self, supported_extensions: Set[str]):
        """
        Initialize a new extractor

        Args:
            supported_extensions: Set of supported file extensions (without dot)
        """
        self.supported_extensions = supported_extensions
        self.logger = logging.getLogger(self.__class__.__name__)

    def can_extract(self, file_name: str) -> bool:
        """
        Check if this extractor can handle the given file type

        Args:
            file_name: Name of the file to check

        Returns:
            True if this extractor can handle the file, False otherwise
        """
        if not file_name:
            return False

        extension = file_name.split(".")[-1].lower() if "." in file_name else ""
        return extension in self.supported_extensions
