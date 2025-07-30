#!/usr/bin/env python3

import sys
from pathlib import Path

# Add the current directory to Python path
current_dir = Path(__file__).parent
sys.path.insert(0, str(current_dir))

# Test imports
try:
    from app.domain.models.resume import CVModel, Experience, Training
    print("✅ Domain models import successful")
    
    from app.main import app
    print("✅ FastAPI app import successful")
    
    print("🎉 All imports working correctly!")
    
except ImportError as e:
    print(f"❌ Import error: {e}")
    sys.exit(1)
