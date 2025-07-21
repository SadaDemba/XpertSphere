from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from app.api.router import router as api_router
from app.core.exceptions import BaseApplicationError
from app.core.config import settings
import logging
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)

logging.getLogger("uvicorn").setLevel(logging.INFO)
# Initialize logging
logger = logging.getLogger(__name__)
logger.info(
    f"Starting XpertSphere CV Analyzer API, {settings.API_KEY} loaded from environment variables"
)

# Create FastAPI application
app = FastAPI(
    title="XpertSphere CV Analyzer",
    description="API for extracting and analyzing information from CVs",
    version="1.0.0",
    openapi_tags=[
        {
            "name": "CV Processing",
            "description": "Operations related to CV analysis and information extraction",
        },
        {
            "name": "Health",
            "description": "Health check endpoints",
        },
    ],
    # Customize OpenAPI docs
    docs_url="/docs",
    redoc_url="/redoc",
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Allows all origins in development
    allow_credentials=True,
    allow_methods=["*"],  # Allows all methods
    allow_headers=["*"],  # Allows all headers
)

# Include API router
app.include_router(api_router, prefix="/api")


# Exception handler for application errors
@app.exception_handler(BaseApplicationError)
async def application_exception_handler(request: Request, exc: BaseApplicationError):
    return JSONResponse(
        status_code=422,
        content={"detail": str(exc)},
    )


@app.get("/")
def read_root():
    return {"message": "Welcome to the XpertSphere CV Analyzer API!"}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)
