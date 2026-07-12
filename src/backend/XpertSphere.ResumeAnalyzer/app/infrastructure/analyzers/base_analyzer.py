from abc import ABC, abstractmethod
from typing import Any, Dict, Optional
import json
from app.domain.interfaces import TextAnalyzer
from app.domain.models import CVModel, Experience, Training
from app.core import AnalysisError
import logging


class BaseAnalyzer(TextAnalyzer, ABC):
    """
    Base class for text analyzers
    """

    def __init__(self):
        """Initialize a new analyzer"""
        self.logger = logging.getLogger(self.__class__.__name__)

    async def analyze(
        self, text: str, options: Optional[Dict[str, Any]] = None
    ) -> CVModel:
        """
        Analyze text and extract structured CV information

        Args:
            text: Text to analyze
            options: Optional parameters for the analyzer

        Returns:
            Structured CV model

        Raises:
            AnalysisError: If analysis fails
        """
        try:
            prompt = self._create_prompt(text)

            content = self._get_completion(prompt)

            return self._parse_response(content)

        except Exception as e:
            self.logger.error(f"Error analyzing CV text: {str(e)}")
            raise AnalysisError(f"Failed to analyze CV: {str(e)}")

    @abstractmethod
    def _get_completion(self, prompt: str) -> str:
        """
        Get the completion content from the LLM provider

        Args:
            prompt: Prompt to send to the LLM

        Returns:
            Raw text content of the LLM response
        """
        pass

    def _parse_response(self, content: str) -> CVModel:
        """
        Parse the LLM JSON response into a CVModel

        Args:
            content: Raw JSON text content returned by the LLM

        Returns:
            Structured CV model
        """
        # Parse JSON and create CVModel object
        parsed_data = json.loads(content)

        # Transform each experience into Experience object
        parsed_data["experiences"] = [
            Experience(**exp) for exp in parsed_data["experiences"]
        ]

        parsed_data["trainings"] = [
            Training(**training) for training in parsed_data["trainings"]
        ]

        return CVModel(**parsed_data)

    def _create_prompt(self, text: str) -> str:
        """
        Create the prompt for the LLM

        Args:
            text: Text to analyze

        Returns:
            Formatted prompt
        """
        return f"""
            Analyze this CV and extract PRECISELY the following information.
            Never mix EDUCATION and PROFESSIONAL EXPERIENCES!

            IMPORTANT RULES:
            1. EDUCATION = schools, universities, degrees, academic programs
            2. EXPERIENCES = jobs, internships, professional missions
            3. SKILLS = technologies, languages, tools, personal qualities
            4. If information is not present, use an empty string or empty array
            5. For dates, keep the original format from the CV
            6. For skills, group by logical categories

            CV to analyze:
            {text}

            Respond ONLY with this exact JSON format:
            {{
                "first_name": "person's first name",
                "last_name": "person's last name",
                "email": "email address",
                "phone_number": "phone number",
                "profession": "main professional title",
                "address": "complete address",
                "languages": ["language1 (level)", "language2 (level)"],
                "trainings": [{{
                    "school": "institution name",
                    "level": "degree level (e.g.: Master, Bachelor, High School)",
                    "period": "period (e.g.: 2023/2025)",
                    "field": "field of study"
                }}],
                "skills": [
                    "Frameworks: list of frameworks",
                    "Programming Languages: list of languages",
                    "Databases: list of DBMS",
                    "DevOps Tools: list of tools",
                    "Personal Qualities: list of soft skills"
                ],
                "experiences": [{{
                    "title": "job position",
                    "company": "company name",
                    "location": "company location",
                    "date": "period (original CV format)",
                    "description": "detailed description of missions and responsibilities"
                }}]
            }}

            EXAMPLES of what to distinguish:
            - EDUCATION: "Master in Software Engineering at Ynov Campus"
            - EXPERIENCE: "Software Development Engineer at Expertime"
            - SKILL: "C#, Python, Docker"
            """
