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
        # No words -> single-column path -> plain extract_text()
        mock_page.extract_words.return_value = []

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
        mock_page.extract_words.return_value = []

        mock_pdf = MagicMock()
        mock_pdf.pages = [mock_page]
        mock_pdfplumber.open.return_value.__enter__.return_value = mock_pdf

        # Test
        with pytest.raises(ExtractionError):
            await pdf_extractor.extract_text(b"fake pdf content", "test.pdf")


class _FakePage:
    """Minimal pdfplumber-page stand-in for gutter-detection unit tests."""

    def __init__(self, words, width):
        self._words = words
        self.width = width

    def extract_words(self):
        return self._words


def _word(x0, x1):
    return {"x0": x0, "x1": x1, "text": "x"}


class TestColumnGutterDetection:
    """Unit tests for the two-column gutter detection."""

    @pytest.fixture
    def pdf_extractor(self):
        return PDFExtractor()

    def test_two_column_layout_detects_gutter(self, pdf_extractor):
        """Two word clusters with an empty central band -> a split in between."""
        width = 595
        # Left column x in [10, 100], right column x in [300, 500], on many rows
        words = [_word(10, 100) for _ in range(20)] + [_word(300, 500) for _ in range(20)]
        split_x = pdf_extractor._detect_column_gutter(_FakePage(words, width))
        assert split_x is not None
        assert 100 < split_x < 300

    def test_single_column_layout_returns_none(self, pdf_extractor):
        """Words spanning the central band -> no gutter -> None (fallback)."""
        width = 595
        words = [_word(50, 550) for _ in range(20)]
        assert pdf_extractor._detect_column_gutter(_FakePage(words, width)) is None

    def test_empty_page_returns_none(self, pdf_extractor):
        """No words at all -> None (fallback to plain extraction)."""
        assert pdf_extractor._detect_column_gutter(_FakePage([], 595)) is None
