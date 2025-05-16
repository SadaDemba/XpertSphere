from fastapi import Security, HTTPException, status
from fastapi.security import APIKeyHeader
from app.core.config import settings

# API key header definition
api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)


async def verify_api_key(api_key: str = Security(api_key_header)) -> str:
    """
    Verify that the provided API key is valid

    Args:
        api_key: The API key from the X-API-Key header

    Returns:
        The API key if valid

    Raises:
        HTTPException: If the API key is invalid
    """

    if api_key == settings.API_KEY:
        return api_key
    elif settings.API_KEY == "TODO":
        return api_key

    raise HTTPException(
        status_code=status.HTTP_403_FORBIDDEN,
        detail="Invalid or missing API key",
    )
