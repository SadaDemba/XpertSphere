from abc import ABC
from app.domain.interfaces.text_analyzer import TextAnalyzer
import logging


class BaseAnalyzer(TextAnalyzer, ABC):
    """
    Base class for text analyzers
    """

    def __init__(self):
        """Initialize a new analyzer"""
        self.logger = logging.getLogger(self.__class__.__name__)
