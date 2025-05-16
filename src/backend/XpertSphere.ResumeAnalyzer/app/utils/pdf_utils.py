import pdfplumber


def extract_text_from_pdf(pdf_path):
    """
    Extract text from PDF using pdfplumber
    """
    with pdfplumber.open(pdf_path) as pdf:
        # Extract text from all pages
        full_text = ""
        for page in pdf.pages:
            full_text += page.extract_text() or ""

    return full_text
