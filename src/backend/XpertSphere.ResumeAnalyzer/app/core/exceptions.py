class BaseApplicationError(Exception):
    """Base class for application errors"""

    pass


class ExtractionError(BaseApplicationError):
    """Raised when document text extraction fails"""

    pass


class AnalysisError(BaseApplicationError):
    """Raised when text analysis fails"""

    pass


class ValidationError(BaseApplicationError):
    """Raised when data validation fails"""

    pass
