import os
from typing import Optional
from pydantic_settings import BaseSettings
from pydantic import ConfigDict
from azure.keyvault.secrets import SecretClient
from azure.identity import DefaultAzureCredential


class Settings(BaseSettings):
    """
    Application settings
    """
    
    KEY_VAULT_URL: Optional[str] = None
    ENVIRONMENT: str = "development"

    # Azure OpenAI configuration
    AZURE_OPENAI_ENDPOINT: Optional[str] = None
    AZURE_OPENAI_API_KEY: Optional[str] = None
    AZURE_OPENAI_API_VERSION: str = "2024-12-01-preview"
    AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO: str = "gpt-35-turbo"
    AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO: str = "0125"
    AZURE_OPENAI_TEMPERATURE: float = 0.2

    # File size limits
    MAX_FILE_SIZE_MB: int = 10
    
    def __init__(self, **kwargs):
        super().__init__(**kwargs)

        if self.ENVIRONMENT == "production" and self.KEY_VAULT_URL:
                self._load_from_keyvault()
                
    def _load_from_keyvault(self):
        """Load secrets from Azure Key Vault"""
        try:
            credential = DefaultAzureCredential()
            client = SecretClient(vault_url=self.KEY_VAULT_URL, credential=credential)
            
            self.AZURE_OPENAI_ENDPOINT = client.get_secret("azure-openai-endpoint").value
            self.AZURE_OPENAI_API_KEY = client.get_secret("azure-openai-api-key").value
            self.AZURE_OPENAI_API_VERSION = client.get_secret("azure-openai-api-version").value
            self.AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO = client.get_secret("azure-openai-deployment-gpt35").value
            self.AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO = client.get_secret("azure-openai-model-version-gpt35").value
            self.AZURE_OPENAI_TEMPERATURE = float(client.get_secret("azure-openai-temperature").value)
            
            print("Secrets loaded from Azure Key Vault")
        except Exception as e:
            print(f"Failed to load secrets from Key Vault: {e}")
            print("Falling back to environment variables")
            
    model_config = ConfigDict(
        env_file=".env",
        case_sensitive=True,
        extra="ignore"
    )


# Create settings instance
settings = Settings()
