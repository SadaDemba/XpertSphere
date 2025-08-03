from typing import Optional
from pydantic_settings import BaseSettings
from pydantic import ConfigDict
from azure.keyvault.secrets import SecretClient
from azure.identity import DefaultAzureCredential, ManagedIdentityCredential
import logging

logger = logging.getLogger(__name__)
class Settings(BaseSettings):
    """
    Application settings
    """
    
    KEY_VAULT_URL: Optional[str] = None
    AZURE_CLIENT_ID: Optional[str] = None
    ENVIRONMENT: str = "development"

    # Azure OpenAI configuration
    AZURE_OPENAI_ENDPOINT: Optional[str] = None
    AZURE_OPENAI_API_KEY: Optional[str] = None
    AZURE_OPENAI_API_VERSION: str = "2024-12-01-preview"
    
    AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO: str = "gpt-35-turbo"
    AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO: str = "0125"
    
    AZURE_OPENAI_DEPLOYMENT_GPT_4O_MINI_PROD: str = "gpt-4o-mini"
    AZURE_OPENAI_MODEL_VERSION_GPT_4O_MINI_PROD: str = "2024-07-18"
    
    AZURE_OPENAI_TEMPERATURE: float = 0.1

    # File size limits
    MAX_FILE_SIZE_MB: int = 10
    
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        logger.info(f"🔧 Settings initialization - ENVIRONMENT: {self.ENVIRONMENT}")
        logger.info(f"🔧 Settings initialization - KEY_VAULT_URL: {self.KEY_VAULT_URL}")
        logger.info(f"🔧 Settings initialization - AZURE_OPENAI_ENDPOINT: {'***SET***' if self.AZURE_OPENAI_ENDPOINT else 'NOT SET'}")
        logger.info(f"🔧 Settings initialization - AZURE_OPENAI_API_KEY: {'***SET***' if self.AZURE_OPENAI_API_KEY else 'NOT SET'}")

        if self.ENVIRONMENT in ["production", "staging"] and self.KEY_VAULT_URL:
                self._load_from_keyvault()
                
    def _load_from_keyvault(self):
        """Load secrets from Azure Key Vault"""
        try:
            
            if self.AZURE_CLIENT_ID:
                credential = ManagedIdentityCredential(client_id=self.AZURE_CLIENT_ID)
            else:
                credential = DefaultAzureCredential()
            
            
            client = SecretClient(vault_url=self.KEY_VAULT_URL, credential=credential)
            
            # Load secrets with fallback to current values
            try:
                self.AZURE_OPENAI_ENDPOINT = client.get_secret("azure-openai-endpoint").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-endpoint from Key Vault")
                
            try:
                self.AZURE_OPENAI_API_KEY = client.get_secret("azure-openai-api-key").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-api-key from Key Vault")
                
            try:
                self.AZURE_OPENAI_API_VERSION = client.get_secret("azure-openai-api-version").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-api-version from Key Vault")
                
            try:
                self.AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO = client.get_secret("azure-openai-deployment-gpt35").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-deployment-gpt35 from Key Vault")
                
            try:
                self.AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO = client.get_secret("azure-openai-model-version-gpt35").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-model-version-gpt35 from Key Vault")
                
            try:
                self.AZURE_OPENAI_DEPLOYMENT_GPT_4O_MINI_PROD = client.get_secret("azure-openai-deployment-gpt4o-mini").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-deployment-gpt4o-mini from Key Vault")
                
            try:
                self.AZURE_OPENAI_MODEL_VERSION_GPT_4O_MINI_PROD = client.get_secret("azure-openai-model-version-gpt4o-mini").value
            except Exception:
                logger.info("Warning: Could not load azure-openai-model-version-gpt4o-mini from Key Vault")
                
            try:
                self.AZURE_OPENAI_TEMPERATURE = float(client.get_secret("azure-openai-temperature").value)
            except Exception:
                logger.info("Warning: Could not load azure-openai-temperature from Key Vault")
            
            logger.info("Secrets loaded from Azure Key Vault")
            logger.info(f"🔧 After Key Vault - AZURE_OPENAI_ENDPOINT: {'***SET***' if self.AZURE_OPENAI_ENDPOINT else 'NOT SET'}")
            logger.info(f"🔧 After Key Vault - AZURE_OPENAI_API_KEY: {'***SET***' if self.AZURE_OPENAI_API_KEY else 'NOT SET'}")
        except Exception as e:
            logger.info(f"Failed to load secrets from Key Vault: {e}")
            logger.info("Falling back to environment variables")
   
    @property
    def current_deployment(self) -> str:
        """Get the deployment based on environment"""
        if self.ENVIRONMENT == "production":
            return self.AZURE_OPENAI_DEPLOYMENT_GPT_4O_MINI_PROD
        else:  # staging/development
            return self.AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO

    @property  
    def current_model_version(self) -> str:
        """Get the model version based on environment"""
        if self.ENVIRONMENT == "production":
            return self.AZURE_OPENAI_MODEL_VERSION_GPT_4O_MINI_PROD
        else:  # staging/development
            return self.AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO
            
    model_config = ConfigDict(
        env_file=".env",
        case_sensitive=True,
        extra="ignore"
    )


# Create settings instance
settings = Settings()
