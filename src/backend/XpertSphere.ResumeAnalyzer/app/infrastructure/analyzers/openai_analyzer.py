from openai import AzureOpenAI
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

    def _get_completion(self, prompt: str) -> str:
        """
        Get the completion content from Azure OpenAI

        Args:
            prompt: Prompt to send to the LLM

        Returns:
            Raw text content of the LLM response
        """
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

        return response.choices[0].message.content
