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
            1. EDUCATION = schools, universities, degrees, academic/diploma programs (the place where a diploma is prepared).
            2. EXPERIENCES = jobs, professional missions, internships (stage) AND work-study contracts (alternance, apprentissage, contrat de professionnalisation). An internship or a work-study contract is ALWAYS a professional experience, NEVER an education entry, even when it prepares or is attached to a diploma. The diploma itself goes in EDUCATION; the work performed goes in EXPERIENCES.
            3. SKILLS = technologies, languages, tools, personal qualities.
            4. If information is not present, use an empty string or empty array.
            5. NORMALIZE every date, keeping the finest granularity available in the CV: "DD/MM/YYYY - DD/MM/YYYY" when the day is given, "MM/YYYY - MM/YYYY" when only month/year, "YYYY - YYYY" when only the year (parts zero-padded, " - " between start and end). Use "... - en cours" when the period is still ongoing ("depuis", "present", "aujourd'hui", "en cours"). Convert any original notation (e.g. "sept. 2024", "09.2024", "12 mars 2022") to this numeric format.
            6. For skills, group by logical categories.

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
                    "period": "period, normalized per rule 5 (e.g.: 2023 - 2025)",
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
                    "date": "period, normalized per rule 5 (e.g.: 09/2024 - 10/2025)",
                    "description": "detailed description of missions and responsibilities"
                }}]
            }}

            EXAMPLES of correct classification (French CVs):
            - EDUCATION: "Master Génie Logiciel — Ynov Campus (2023/2025)" -> trainings, period "2023 - 2025"
            - EXPERIENCE (alternance): "Ingénieur Études & Développement en alternance chez Expertime, sept. 2024 - oct. 2025" -> experiences, title "Ingénieur Études & Développement (Alternance)", date "09/2024 - 10/2025"
            - EXPERIENCE (stage): "Développeur full-stack (Stage) chez Dynamiqs, oct. 2022 - oct. 2023" -> experiences, date "10/2022 - 10/2023"
            - SKILL: "C#, Python, Docker" -> skills
            """
