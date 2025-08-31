using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Utils.Results;

/// <summary>
/// Authentication result that extends ServiceResult for auth-specific operations
/// </summary>
public class AuthResult : ServiceResult<AuthResponseDto>
{
    private AuthResult() : base() { }
    
    /// <summary>
    /// Additional metadata for this authentication result
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();

    public static AuthResult Success(string message = "Operation successful", string? returnUrl = null)
    {
        var response = new AuthResponseDto
        {
            RedirectUrl = returnUrl
        };

        return new AuthResult
        {
            IsSuccess = true,
            Data = response,
            Message = message,
            StatusCode = 200
        };
    }

    public static AuthResult SuccessWithUser(AuthResponseDto user, string message = "Operation successful")
    {

        return new AuthResult
        {
            IsSuccess = true,
            Data = user,
            Message = message,
            StatusCode = 200
        };
    }

    public static AuthResult Failure(string message, List<string>? errors = null, int statusCode = 400)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Message = message,
            Errors = errors ?? [message],
            StatusCode = statusCode
        };
    }

    public new static AuthResult Unauthorized(string message = "Unauthorized")
    {
        return new AuthResult
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 401
        };
    }

    public new static AuthResult Conflict(string message)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 409
        };
    }

    public new static AuthResult ValidationError(List<string> validationErrors)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Message = "Validation failed",
            Errors = validationErrors,
            StatusCode = 422
        };
    }

    public static AuthResult SuccessWithData(object data, string message = "Operation successful")
    {
        return new AuthResult
        {
            IsSuccess = true,
            Data = data as AuthResponseDto,
            Message = message,
            StatusCode = 200
        };
    }
    
    /// <summary>
    /// Add metadata to this authentication result
    /// </summary>
    public AuthResult WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
    
    /// <summary>
    /// Set the status code for this authentication result
    /// </summary>
    public AuthResult WithStatusCode(int statusCode)
    {
        StatusCode = statusCode;
        return this;
    }
    
    /// <summary>
    /// Get metadata value by key
    /// </summary>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
    
    /// <summary>
    /// Check if metadata contains a specific key
    /// </summary>
    public bool HasMetadata(string key)
    {
        return Metadata.ContainsKey(key);
    }
}