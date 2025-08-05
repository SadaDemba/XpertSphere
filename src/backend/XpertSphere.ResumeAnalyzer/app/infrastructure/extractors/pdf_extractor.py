import pdfplumber
import tempfile
import os
from app.infrastructure.extractors import BaseExtractor
from app.core import ExtractionError


class PDFExtractor(BaseExtractor):
    """
    PDF document extractor using pdfplumber
    """

    def __init__(self):
        """Initialize the PDF extractor"""
        super().__init__(supported_extensions={"pdf"})

    async def extract_text(self, file_content: bytes, file_name: str) -> str:
        """
        Extract text from a PDF document

        Args:
            file_content: Binary content of the PDF file
            file_name: Name of the file

        Returns:
            Extracted text

        Raises:
            ExtractionError: If extraction fails
        """
        # Create a temporary file
        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as temp_file:
            temp_file.write(file_content)
            temp_path = temp_file.name

        try:
            with pdfplumber.open(temp_path) as pdf:
                # Extract text from all pages
                full_text = ""
                for page in pdf.pages:
                    text = page.extract_text()
                    if text:
                        full_text += text + "\n"

            if not full_text or full_text.isspace():
                self.logger.warning(f"Failed to extract text from PDF: {file_name}")
                raise ExtractionError(f"Could not extract text from PDF: {file_name}")

            return full_text
        except Exception as e:
            self.logger.error(f"Error extracting text from PDF: {str(e)}")
            raise ExtractionError(f"Failed to extract text from PDF: {str(e)}")
        finally:
            # Clean up the temporary file
            os.unlink(temp_path)
