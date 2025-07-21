import os
from pydantic_settings import BaseSettings
from typing import Optional


class Settings(BaseSettings):
    """
    Application settings
    """

    # API configuration
    API_KEY: str = os.environ.get("API_KEY", "default-dev-key")

    # Azure OpenAI configuration
    AZURE_OPENAI_ENDPOINT: str = os.environ.get("AZURE_OPENAI_ENDPOINT", "")
    AZURE_OPENAI_API_KEY: str = os.environ.get("AZURE_OPENAI_API_KEY", "")
    AZURE_OPENAI_API_VERSION: str = os.environ.get("AZURE_OPENAI_API_VERSION", "")
    AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO: str = os.environ.get(
        "AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO", ""
    )
    AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO: str = os.environ.get(
        "AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO", ""
    )
    AZURE_OPENAI_TEMPERATURE: str = os.environ.get("AZURE_OPENAI_TEMPERATURE", "0.7")

    # File size limits
    MAX_FILE_SIZE_MB: int = 3

    class Config:
        env_file = ".env"
        case_sensitive = True
        extra = (
            "ignore"  # Permet d'ignorer les variables d'environnement supplémentaires
        )


# Create settings instance
settings = Settings()
