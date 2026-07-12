from openai import AzureOpenAI, OpenAI
from app.core import settings


def get_llm() -> AzureOpenAI:
    """
    Initialize and return Azure OpenAI client
    """
    return AzureOpenAI(
        azure_endpoint=settings.AZURE_OPENAI_ENDPOINT,
        api_key=settings.AZURE_OPENAI_API_KEY,
        api_version=settings.AZURE_OPENAI_API_VERSION,
        azure_deployment=settings.current_deployment,
    )


def get_groq_llm() -> OpenAI:
    """
    Initialize and return Groq client (OpenAI-compatible SDK)
    """
    return OpenAI(
        base_url=settings.GROQ_BASE_URL,
        api_key=settings.GROQ_API_KEY,
    )