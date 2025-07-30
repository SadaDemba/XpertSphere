import pytest
import json
from unittest.mock import patch, MagicMock
from app.infrastructure.analyzers.openai_analyzer import OpenAIAnalyzer
from app.domain.models.resume import CVModel
from app.core.exceptions import AnalysisError


class TestOpenAIAnalyzer:
    """Basic tests for OpenAI analyzer"""
    
    @pytest.fixture
    def openai_analyzer(self):
        """Create an OpenAI analyzer with mocked client"""
        with patch('app.infrastructure.analyzers.openai_analyzer.AzureOpenAI') as mock_client:
            analyzer = OpenAIAnalyzer()
            analyzer.client = mock_client.return_value
            return analyzer
    
    @pytest.mark.asyncio
    async def test_analyze_success(self, openai_analyzer):
        """Test successful CV analysis"""
        # Mock OpenAI response
        mock_response = MagicMock()
        mock_response.choices = [MagicMock()]
        mock_response.choices[0].message.content = json.dumps({
            "first_name": "John",
            "last_name": "Doe",
            "email": "john.doe@example.com",
            "phone_number": "+1234567890",
            "profession": "Software Engineer",
            "address": "123 Main St",
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
        
        openai_analyzer.client.chat.completions.create.return_value = mock_response
        
        # Test
        result = await openai_analyzer.analyze("Sample CV text")
        
        # Assertions
        assert isinstance(result, CVModel)
        assert result.first_name == "John"
        assert result.last_name == "Doe"
        assert result.email == "john.doe@example.com"
        assert len(result.experiences) == 1
        assert len(result.trainings) == 1
    
    @pytest.mark.asyncio
    async def test_analyze_api_error(self, openai_analyzer):
        """Test analysis when OpenAI API fails"""
        openai_analyzer.client.chat.completions.create.side_effect = Exception("API Error")
        
        with pytest.raises(AnalysisError):
            await openai_analyzer.analyze("Sample CV text")
    
    @pytest.mark.asyncio
    async def test_analyze_invalid_json(self, openai_analyzer):
        """Test analysis with invalid JSON response"""
        mock_response = MagicMock()
        mock_response.choices = [MagicMock()]
        mock_response.choices[0].message.content = "Invalid JSON"
        
        openai_analyzer.client.chat.completions.create.return_value = mock_response
        
        with pytest.raises(AnalysisError):
            await openai_analyzer.analyze("Sample CV text")
