from typing import Any, Dict, Optional
import os
import json
from openai import AzureOpenAI
from app.domain.models.resume import CVModel, Experience, Training
from app.infrastructure.analyzers.base_analyzer import BaseAnalyzer
from app.core.exceptions import AnalysisError
from app.core.config import settings


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
            self.logger.info(f"AZURE_OPENAI_ENDPOINT: {'***SET***' if settings.AZURE_OPENAI_ENDPOINT else 'NOT SET'}")
            self.logger.info(f"AZURE_OPENAI_API_KEY: {'***SET***' if settings.AZURE_OPENAI_API_KEY else 'NOT SET'}")
            self.logger.info(f"AZURE_OPENAI_API_VERSION: {settings.AZURE_OPENAI_API_VERSION}")
            
            # Vérifier que les credentials sont présents
            if not settings.AZURE_OPENAI_ENDPOINT:
                raise ValueError("AZURE_OPENAI_ENDPOINT is not set")
            if not settings.AZURE_OPENAI_API_KEY:
                raise ValueError("AZURE_OPENAI_API_KEY is not set")
                
            return AzureOpenAI(
                azure_endpoint=settings.AZURE_OPENAI_ENDPOINT,
                api_key=settings.AZURE_OPENAI_API_KEY,
                api_version=settings.AZURE_OPENAI_API_VERSION,
                azure_deployment=settings.current_deployment,
            )
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
                model="gpt-35-turbo",
                messages=[
                    {
                        "role": "system",
                        "content": "You are a specialized assistant for CV information extraction.",
                    },
                    {"role": "user", "content": prompt},
                ],
                response_format={"type": "json_object"},
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
        Extract precisely the following information from the CV:
        - First name
        - Last name
        - Email
        - Phone number
        - Profession
        - Address
        - Languages
        - Education/Training
        - Skills
        - Professional experiences

        CV text:
        {text}

        Respond ONLY in the following JSON format. If information is missing, use an empty string or empty array:
        {{
            "first_name": "",
            "last_name": "",
            "email": "",
            "phone_number": "",
            "profession": "",
            "address": "",
            "languages": [],
            "trainings": [{{
                "school": "",
                "level": "",
                "period": ""
            }}],
            "skills": [],
            "experiences": [{{
                "title": "",
                "description": "",
                "date": ""
            }}]
        }}

        The 'date' field may be missing, in which case use an empty string. If present, it should be in one of the following formats:
         - 'dd mm yyyy' (if you have day, month and year);
         - 'mm yyyy' (if you don't have the day);
         - 'yyyy' (if you only see the year);
        """
