import pdfplumber
import tempfile
import os
from app.infrastructure.extractors import BaseExtractor
from app.core import ExtractionError


class PDFExtractor(BaseExtractor):
    """
    PDF document extractor using pdfplumber.

    Two-column layouts (common on modern CVs with a sidebar) are extracted
    column by column so that a sidebar heading is never glued onto the main
    column on the same physical line. Single-column pages are extracted with
    the default behaviour, unchanged.
    """

    # Central horizontal band (as fractions of the page width) in which the
    # gutter of a two-column layout is expected. Looking only in the middle
    # avoids mistaking an outer margin for a column separator.
    _GUTTER_BAND = (0.20, 0.60)
    # A gutter narrower than this fraction of the page width is treated as
    # noise (i.e. a single-column page), not a real column separator.
    _MIN_GUTTER_FRAC = 0.03

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
                    text = self._extract_page_text(page)
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

    def _extract_page_text(self, page) -> str:
        """
        Extract a single page's text, handling two-column layouts.

        When a clear vertical gutter is detected, each column is extracted
        independently and the columns are concatenated (left/sidebar first,
        then the main column). Otherwise the default single-column extraction
        is used unchanged.

        Args:
            page: A pdfplumber page

        Returns:
            The page text
        """
        split_x = self._detect_column_gutter(page)
        if split_x is None:
            return page.extract_text() or ""

        try:
            left = page.crop((0, 0, split_x, page.height)).extract_text() or ""
            right = page.crop((split_x, 0, page.width, page.height)).extract_text() or ""
        except Exception:
            # Defensive: never let column cropping break a page that the
            # default single-column extraction could otherwise handle.
            self.logger.warning("Column cropping failed; falling back to plain extraction")
            return page.extract_text() or ""

        columns = [c for c in (left, right) if c and not c.isspace()]
        return "\n".join(columns)

    def _detect_column_gutter(self, page):
        """
        Detect the vertical gutter separating two columns.

        The gutter is the widest vertical band, within the central region of
        the page, that no word occupies. Returns its x coordinate, or None when
        the page looks single-column (no sufficiently wide central gutter).

        Args:
            page: A pdfplumber page

        Returns:
            The x coordinate of the gutter, or None
        """
        try:
            words = page.extract_words()
        except Exception:
            return None
        if not words:
            return None

        width = page.width
        # occupancy[x] = number of words covering the horizontal position x
        occupancy = [0] * (int(width) + 2)
        for w in words:
            start = max(0, int(w["x0"]))
            end = min(len(occupancy) - 1, int(w["x1"]))
            for x in range(start, end + 1):
                occupancy[x] += 1

        lo = int(width * self._GUTTER_BAND[0])
        hi = int(width * self._GUTTER_BAND[1])
        best_start, best_len, run_start = None, 0, None
        for x in range(lo, hi):
            if occupancy[x] == 0:
                if run_start is None:
                    run_start = x
                if x - run_start > best_len:
                    best_len, best_start = x - run_start, run_start
            else:
                run_start = None

        if best_start is None or best_len < width * self._MIN_GUTTER_FRAC:
            return None
        return best_start + best_len / 2
