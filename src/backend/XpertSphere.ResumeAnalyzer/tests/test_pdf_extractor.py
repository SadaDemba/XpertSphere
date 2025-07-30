import pytest
from unittest.mock import patch, MagicMock
from app.infrastructure.extractors.pdf_extractor import PDFExtractor
from app.core.exceptions import ExtractionError


class TestPDFExtractor:
    """Basic tests for PDF extractor"""
    
    @pytest.fixture
    def pdf_extractor(self):
        """Create a PDF extractor instance"""
        return PDFExtractor()
    
    def test_can_extract_pdf(self, pdf_extractor):
        """Test that extractor can handle PDF files"""
        assert pdf_extractor.can_extract("document.pdf") is True
        assert pdf_extractor.can_extract("document.PDF") is True
    
    def test_cannot_extract_other_formats(self, pdf_extractor):
        """Test that extractor rejects non-PDF files"""
        assert pdf_extractor.can_extract("document.txt") is False
        assert pdf_extractor.can_extract("document.docx") is False
        assert pdf_extractor.can_extract("document") is False
    
    @pytest.mark.asyncio
    @patch('app.infrastructure.extractors.pdf_extractor.pdfplumber')
    @patch('tempfile.NamedTemporaryFile')
    @patch('os.unlink')
    async def test_extract_text_success(self, mock_unlink, mock_temp_file, mock_pdfplumber, pdf_extractor):
        """Test successful text extraction from PDF"""
        # Setup mocks
        mock_temp_file.return_value.__enter__.return_value.name = "/tmp/test.pdf"
        mock_temp_file.return_value.__enter__.return_value.write = MagicMock()
        
        mock_page = MagicMock()
        mock_page.extract_text.return_value = "Sample PDF text"
        
        mock_pdf = MagicMock()
        mock_pdf.pages = [mock_page]
        mock_pdfplumber.open.return_value.__enter__.return_value = mock_pdf
        
        # Test
        result = await pdf_extractor.extract_text(b"fake pdf content", "test.pdf")
        
        # Assertions
        assert result == "Sample PDF text\n"
        mock_unlink.assert_called_once()
    
    @pytest.mark.asyncio
    @patch('app.infrastructure.extractors.pdf_extractor.pdfplumber')
    @patch('tempfile.NamedTemporaryFile')
    @patch('os.unlink')
    async def test_extract_text_empty_pdf(self, mock_unlink, mock_temp_file, mock_pdfplumber, pdf_extractor):
        """Test extraction from empty PDF raises error"""
        # Setup mocks
        mock_temp_file.return_value.__enter__.return_value.name = "/tmp/test.pdf"
        mock_temp_file.return_value.__enter__.return_value.write = MagicMock()
        
        mock_page = MagicMock()
        mock_page.extract_text.return_value = ""
        
        mock_pdf = MagicMock()
        mock_pdf.pages = [mock_page]
        mock_pdfplumber.open.return_value.__enter__.return_value = mock_pdf
        
        # Test
        with pytest.raises(ExtractionError):
            await pdf_extractor.extract_text(b"fake pdf content", "test.pdf")
