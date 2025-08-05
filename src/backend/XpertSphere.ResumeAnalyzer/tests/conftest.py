import os
import sys
from pathlib import Path

# Get the project root directory
project_root = Path(__file__).parent.parent

# Add project root to Python path
if str(project_root) not in sys.path:
    sys.path.insert(0, str(project_root))

# Set test environment variables
os.environ["AZURE_OPENAI_ENDPOINT"] = "https://test.openai.azure.com/"
os.environ["AZURE_OPENAI_API_KEY"] = "test-key"
os.environ["AZURE_OPENAI_API_VERSION"] = "2023-05-15"
os.environ["AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO"] = "test-deployment"
os.environ["AZURE_OPENAI_MODEL_VERSION_GPT_35_TURBO"] = "0613"
os.environ["AZURE_OPENAI_TEMPERATURE"] = "0.7"
os.environ["MAX_FILE_SIZE_MB"] = "3"
