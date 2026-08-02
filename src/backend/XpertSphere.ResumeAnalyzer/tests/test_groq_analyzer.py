import pytest
import json
from unittest.mock import patch, MagicMock
from app.infrastructure.analyzers.groq_analyzer import GroqAnalyzer
from app.domain.models.resume import CVModel
from app.core.exceptions import AnalysisError


class TestGroqAnalyzer:
    """Basic tests for Groq analyzer"""

    @pytest.fixture
    def groq_analyzer(self):
        """Create a Groq analyzer with mocked client"""
        with patch('app.infrastructure.analyzers.groq_analyzer.OpenAI') as mock_client:
            analyzer = GroqAnalyzer()
            analyzer.client = mock_client.return_value
            return analyzer

    @pytest.mark.asyncio
    async def test_analyze_success(self, groq_analyzer):
        """Test successful CV analysis"""
        # Mock Groq response
        mock_response = MagicMock()
        mock_response.choices = [MagicMock()]
        mock_response.choices[0].message.content = json.dumps({
            "first_name": "John",
            "last_name": "Doe",
            "email": "john.doe@example.com",
            "phone_number": "+1234567890",
            "profession": "Software Engineer",
            "address": {
                "street_number": "123",
                "street": "Main St",
                "city": "Springfield",
                "postal_code": "12345",
                "region": "",
                "country": "USA"
            },
            "languages": ["English"],
            "skills": ["Python"],
            "experiences": [{
                "title": "Developer",
                "description": "Coded stuff",
                "date": "2020"
            }],
            "trainings": [{
                "school": "University",
                "level": "Bachelor",
                "period": "2016-2020"
            }]
        })

        groq_analyzer.client.chat.completions.create.return_value = mock_response

        # Test
        result = await groq_analyzer.analyze("Sample CV text")

        # Assertions
        assert isinstance(result, CVModel)
        assert result.first_name == "John"
        assert result.last_name == "Doe"
        assert result.email == "john.doe@example.com"
        assert len(result.experiences) == 1
        assert len(result.trainings) == 1
        # Address is parsed into a structured object
        assert result.address is not None
        assert result.address.city == "Springfield"
        assert result.address.postal_code == "12345"
        assert result.address.country == "USA"

    @pytest.mark.asyncio
    async def test_analyze_api_error(self, groq_analyzer):
        """Test analysis when Groq API fails"""
        groq_analyzer.client.chat.completions.create.side_effect = Exception("API Error")

        with pytest.raises(AnalysisError):
            await groq_analyzer.analyze("Sample CV text")

    @pytest.mark.asyncio
    async def test_analyze_invalid_json(self, groq_analyzer):
        """Test analysis with invalid JSON response"""
        mock_response = MagicMock()
        mock_response.choices = [MagicMock()]
        mock_response.choices[0].message.content = "Invalid JSON"

        groq_analyzer.client.chat.completions.create.return_value = mock_response

        with pytest.raises(AnalysisError):
            await groq_analyzer.analyze("Sample CV text")
