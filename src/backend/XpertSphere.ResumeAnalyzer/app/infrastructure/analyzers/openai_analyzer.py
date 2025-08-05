from typing import Any, Dict, Optional
import json
from openai import AzureOpenAI
from app.domain.models import CVModel, Experience, Training
from app.infrastructure.analyzers import BaseAnalyzer
from app.core import AnalysisError, settings
from app.utils import get_llm



class OpenAIAnalyzer(BaseAnalyzer):
    """
    Text analyzer using Azure OpenAI
    """

    def __init__(self):
        """Initialize the OpenAI analyzer"""
        super().__init__()
        self.client = self._initialize_client()

    def _initialize_client(self) -> AzureOpenAI:
        """
        Initialize the Azure OpenAI client

        Returns:
            Configured Azure OpenAI client
        """
        try:
            # Debug logging
            self.logger.info(f"Initializing Azure OpenAI client...")
            self.logger.info(f"ENVIRONMENT: {settings.ENVIRONMENT}")
            self.logger.info(f"KEY_VAULT_URL: {settings.KEY_VAULT_URL}")
            self.logger.info(f"AZURE_CLIENT_ID: {settings.AZURE_CLIENT_ID}")
            self.logger.info(f"AZURE_OPENAI_ENDPOINT: {settings.AZURE_OPENAI_ENDPOINT if settings.AZURE_OPENAI_ENDPOINT else 'NOT SET'}")
            self.logger.info(f"AZURE_OPENAI_API_KEY: {'***SET***' if settings.AZURE_OPENAI_API_KEY else 'NOT SET'}")
            self.logger.info(f"AZURE_OPENAI_API_VERSION: {settings.AZURE_OPENAI_API_VERSION}")
            
            
            if not settings.AZURE_OPENAI_ENDPOINT:
                raise ValueError("AZURE_OPENAI_ENDPOINT is not set")
            if not settings.AZURE_OPENAI_API_KEY:
                raise ValueError("AZURE_OPENAI_API_KEY is not set")
                
            return get_llm()
        except Exception as e:
            self.logger.error(f"Failed to initialize Azure OpenAI client: {str(e)}")
            raise AnalysisError(f"Failed to initialize Azure OpenAI client: {str(e)}")

    async def analyze(
        self, text: str, options: Optional[Dict[str, Any]] = None
    ) -> CVModel:
        """
        Analyze text and extract structured CV information using OpenAI

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

            response = self.client.chat.completions.create(
                model=settings.current_deployment,
                messages=[
                    {
                        "role": "system",
                        "content": """You are an expert in CV information extraction. 
                        You must carefully analyze the CV and extract ONLY the information that is present.
                        Clearly distinguish between EDUCATION (schools, universities, degrees) and PROFESSIONAL EXPERIENCES (jobs, internships).
                        Be precise and never mix these two categories.""",
                    },
                    {"role": "user", "content": prompt},
                ],
                response_format={"type": "json_object"},
                temperature=settings.AZURE_OPENAI_TEMPERATURE
            )

            # Parse JSON and create CVModel object
            parsed_data = json.loads(response.choices[0].message.content)

            # Transform each experience into Experience object
            parsed_data["experiences"] = [
                Experience(**exp) for exp in parsed_data["experiences"]
            ]

            parsed_data["trainings"] = [
                Training(**training) for training in parsed_data["trainings"]
            ]

            return CVModel(**parsed_data)

        except Exception as e:
            self.logger.error(f"Error analyzing CV text: {str(e)}")
            raise AnalysisError(f"Failed to analyze CV: {str(e)}")

    def _create_prompt(self, text: str) -> str:
        """
        Create the prompt for OpenAI

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