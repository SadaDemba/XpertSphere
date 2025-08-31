# ResumeAnalyzer Tests

Simple test suite for the XpertSphere ResumeAnalyzer API.

## Setup

Install dependencies:

```bash
pip install -r requirements.txt
```

## Running Tests

Run all tests:

```bash
pytest
```

Run specific test files:

```bash
pytest tests/test_models.py
pytest tests/test_pdf_extractor.py
pytest tests/test_openai_analyzer.py
pytest tests/test_api.py
```

Run with verbose output:

```bash
pytest -v
```

## Running the API

Start the development server:

```bash
uvicorn app.main:app --reload --port 8000
```

The API will be available at:

- [http://localhost:8000/docs](http://localhost:8000/docs) (Swagger UI)
- [http://localhost:8000/redoc](http://localhost:8000/redoc) (ReDoc)

## Test Coverage

The tests cover:

- Domain models (CVModel, Experience, Training)
- PDF text extraction with mocking
- OpenAI analyzer with mocked API calls
- Basic API endpoints and validation

All external dependencies are mocked to avoid costs and network calls.
