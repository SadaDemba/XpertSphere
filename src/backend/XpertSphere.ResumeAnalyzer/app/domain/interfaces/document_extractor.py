from abc import ABC, abstractmethod
from typing import BinaryIO, Optional


class DocumentExtractor(ABC):
    """
    Interface for document text extraction
    """

    @abstractmethod
    def can_extract(self, file_name: str) -> bool:
        """
        Check if this extractor can handle the given file type

        Args:
            file_name: Name of the file to check

        Returns:
            True if this extractor can handle the file, False otherwise
        """
        pass

    @abstractmethod
    async def extract_text(self, file_content: bytes, file_name: str) -> str:
        """
        Extract text from a document

        Args:
            file_content: Binary content of the file
            file_name: Name of the file

        Returns:
            Extracted text

        Raises:
            ExtractionError: If extraction fails
        """
        pass
