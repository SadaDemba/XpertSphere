import pytest
from pydantic import ValidationError
from app.core.config import Settings


class TestSettingsLlmProviderValidation:
    """Fail-fast validation of the LLM provider configuration"""

    def test_groq_provider_without_config_raises(self, monkeypatch):
        """Selecting Groq without GROQ_API_KEY/GROQ_MODEL must fail at instantiation"""
        monkeypatch.delenv("GROQ_API_KEY", raising=False)
        monkeypatch.delenv("GROQ_MODEL", raising=False)

        with pytest.raises(ValueError):
            Settings(LLM_PROVIDER="groq", GROQ_API_KEY=None, GROQ_MODEL=None)

    def test_invalid_llm_provider_raises(self):
        """A value outside {azure_openai, groq} must fail at instantiation"""
        with pytest.raises(ValidationError):
            Settings(LLM_PROVIDER="not-a-real-provider")

    def test_groq_provider_with_config_succeeds(self, monkeypatch):
        """Selecting Groq with GROQ_API_KEY/GROQ_MODEL set succeeds"""
        settings = Settings(
            LLM_PROVIDER="groq",
            GROQ_API_KEY="test-key",
            GROQ_MODEL="test-model",
        )

        assert settings.LLM_PROVIDER == "groq"

    def test_default_azure_openai_provider_with_config_succeeds(self):
        """Default provider (azure_openai) with existing test env vars succeeds"""
        settings = Settings()

        assert settings.LLM_PROVIDER == "azure_openai"
