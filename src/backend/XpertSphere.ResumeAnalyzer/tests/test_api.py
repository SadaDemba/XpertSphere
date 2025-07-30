import pytest
from unittest.mock import MagicMock, AsyncMock
from fastapi.testclient import TestClient
from io import BytesIO
from app.main import app
from app.api.dependencies import get_cv_service


class TestAPI:
    """Basic API endpoint tests"""
    
    @pytest.fixture
    def client(self):
        """Create test client"""
        return TestClient(app)
    
    def test_health_check(self, client):
        """Test the root health check endpoint"""
        response = client.get("/")
        
        assert response.status_code == 200
        data = response.json()
        assert "message" in data
        assert "XpertSphere CV Analyzer API" in data["message"]
    
    def test_openapi_docs(self, client):
        """Test OpenAPI documentation endpoint"""
        response = client.get("/openapi.json")
        
        assert response.status_code == 200
        data = response.json()
        assert "openapi" in data
        assert data["info"]["title"] == "XpertSphere CV Analyzer"
    
    def test_extract_endpoint_success(self, client):
        """Test successful CV extraction"""
        from app.domain.models.resume import CVModel
        
        # Override the dependency
        def mock_get_cv_service():
            mock_service = MagicMock()
            mock_service.process_cv = AsyncMock(return_value=CVModel(
                first_name="John",
                last_name="Doe"
            ))
            return mock_service
        
        app.dependency_overrides[get_cv_service] = mock_get_cv_service
        
        try:
            response = client.post(
                "/api/extract/",
                files={"file": ("test.pdf", BytesIO(b"fake pdf content"), "application/pdf")}
            )
            
            assert response.status_code == 200
            data = response.json()
            assert "extracted_data" in data
            assert data["extracted_data"]["first_name"] == "John"
        finally:
            app.dependency_overrides.clear()
    
    def test_extract_endpoint_file_too_large(self, client):
        """Test CV extraction with file too large"""
        # Create 4MB file (exceeds 3MB limit)
        large_content = b"x" * (4 * 1024 * 1024)
        
        response = client.post(
            "/api/extract/",
            files={"file": ("large.pdf", BytesIO(large_content), "application/pdf")}
        )
        
        assert response.status_code == 400
        assert "File too large" in response.json()["detail"]
    
    def test_extract_endpoint_no_file(self, client):
        """Test CV extraction without file"""
        response = client.post("/api/extract/")
        
        # Should return 422 for validation error (missing file)
        assert response.status_code == 422
    
    def test_extract_endpoint_with_raw_text_option(self, client):
        """Test CV extraction with include_raw_text option"""
        from app.domain.models.resume import CVModel
        
        # Override the dependency
        def mock_get_cv_service():
            mock_service = MagicMock()
            mock_service.process_cv = AsyncMock(return_value=CVModel(
                first_name="John",
                last_name="Doe"
            ))
            return mock_service
        
        app.dependency_overrides[get_cv_service] = mock_get_cv_service
        
        try:
            response = client.post(
                "/api/extract/?include_raw_text=true",
                files={"file": ("test.pdf", BytesIO(b"fake pdf content"), "application/pdf")}
            )
            
            assert response.status_code == 200
        finally:
            app.dependency_overrides.clear()
