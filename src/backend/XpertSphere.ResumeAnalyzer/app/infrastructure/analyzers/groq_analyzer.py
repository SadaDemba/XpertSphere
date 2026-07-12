from openai import OpenAI
from app.infrastructure.analyzers import BaseAnalyzer
from app.core import AnalysisError, settings
from app.utils import get_groq_llm



class GroqAnalyzer(BaseAnalyzer):
    """
    Text analyzer using Groq
    """

    def __init__(self):
        """Initialize the Groq analyzer"""
        super().__init__()
        self.client = self._initialize_client()

    def _initialize_client(self) -> OpenAI:
        """
        Initialize the Groq client

        Returns:
            Configured Groq client
        """
        try:
            # Debug logging
            self.logger.info(f"Initializing Groq client...")
            self.logger.info(f"ENVIRONMENT: {settings.ENVIRONMENT}")
            self.logger.info(f"GROQ_BASE_URL: {settings.GROQ_BASE_URL}")
            self.logger.info(f"GROQ_API_KEY: {'***SET***' if settings.GROQ_API_KEY else 'NOT SET'}")
            self.logger.info(f"GROQ_MODEL: {settings.GROQ_MODEL if settings.GROQ_MODEL else 'NOT SET'}")

            if not settings.GROQ_API_KEY:
                raise ValueError("GROQ_API_KEY is not set")
            if not settings.GROQ_MODEL:
                raise ValueError("GROQ_MODEL is not set")

            return get_groq_llm()
        except Exception as e:
            self.logger.error(f"Failed to initialize Groq client: {str(e)}")
            raise AnalysisError(f"Failed to initialize Groq client: {str(e)}")

    def _get_completion(self, prompt: str) -> str:
        """
        Get the completion content from Groq

        Args:
            prompt: Prompt to send to the LLM

        Returns:
            Raw text content of the LLM response
        """
        response = self.client.chat.completions.create(
            model=settings.GROQ_MODEL,
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
            temperature=settings.GROQ_TEMPERATURE
        )

        return response.choices[0].message.content
