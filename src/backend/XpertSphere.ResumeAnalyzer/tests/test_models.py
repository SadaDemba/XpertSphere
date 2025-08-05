import pytest
from app.domain.models.resume import CVModel, Experience, Training


class TestCVModel:
    """Basic tests for CV model"""
    
    def test_cv_model_creation(self):
        """Test creating a CV model with required fields"""
        cv = CVModel(
            first_name="John",
            last_name="Doe"
        )
        
        assert cv.first_name == "John"
        assert cv.last_name == "Doe"
        assert cv.id is not None
    
    def test_cv_model_with_all_fields(self):
        """Test creating a CV model with all fields"""
        cv = CVModel(
            first_name="John",
            last_name="Doe",
            email="john.doe@example.com",
            phone_number="+1234567890",
            profession="Software Engineer",
            languages=["English", "French"],
            skills=["Python", "FastAPI"]
        )
        
        assert cv.email == "john.doe@example.com"
        assert cv.profession == "Software Engineer"
        assert len(cv.languages) == 2
        assert len(cv.skills) == 2
    
    def test_full_name_property(self):
        """Test the full_name property"""
        cv = CVModel(first_name="John", last_name="Doe")
        assert cv.full_name == "John Doe"


class TestExperience:
    """Basic tests for Experience model"""
    
    def test_experience_creation(self):
        """Test creating an experience"""
        exp = Experience(
            title="Senior Developer",
            description="Developed web applications"
        )
        
        assert exp.title == "Senior Developer"
        assert exp.description == "Developed web applications"
        assert exp.company is None
    
    def test_experience_with_all_fields(self):
        """Test creating an experience with all fields"""
        exp = Experience(
            title="Senior Developer",
            description="Developed web applications",
            company="TechCorp",
            location="Paris",
            date="2020-2023"
        )
        
        assert exp.company == "TechCorp"
        assert exp.location == "Paris"
        assert exp.date == "2020-2023"


class TestTraining:
    """Basic tests for Training model"""
    
    def test_training_creation(self):
        """Test creating a training"""
        training = Training(
            school="University",
            level="Master"
        )
        
        assert training.school == "University"
        assert training.level == "Master"
        assert training.period is None
    
    def test_training_with_all_fields(self):
        """Test creating a training with all fields"""
        training = Training(
            school="University of Technology",
            level="Master's Degree",
            period="2018-2020",
            field="Computer Science"
        )
        
        assert training.school == "University of Technology"
        assert training.field == "Computer Science"
