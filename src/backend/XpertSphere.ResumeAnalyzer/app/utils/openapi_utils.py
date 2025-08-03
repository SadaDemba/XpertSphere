from openai import AzureOpenAI
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