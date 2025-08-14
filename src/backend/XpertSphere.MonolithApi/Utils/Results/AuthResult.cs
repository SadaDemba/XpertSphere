using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Utils.Results;

/// <summary>
/// Authentication result that extends ServiceResult for auth-specific operations
/// </summary>
public class AuthResult : ServiceResult<AuthResponseDto>
{
    protected AuthResult() : base() { }

    public new static AuthResult Success(string message = "Operation successful")
    {
        return new AuthResult
        {
            IsSuccess = true,
            Message = message,
            StatusCode = 200
        };
    }

    public static AuthResult SuccessWithUser(User user, string message = "Operation successful", string? emailConfirmationToken = null)
    {
        var response = new AuthResponseDto
        {
            Success = true,
            Message = message,
            EmailConfirmationToken = emailConfirmationToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                OrganizationId = user.OrganizationId,
                OrganizationName = user.Organization?.Name,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                ProfileCompletionPercentage = user.ProfileCompletionPercentage
            }
        };

        return new AuthResult
        {
            IsSuccess = true,
            Data = response,
            Message = message,
            StatusCode = 200
        };
    }

    public static AuthResult SuccessWithTokens(User user, string accessToken, string refreshToken, DateTime tokenExpiry, string message = "Login successful")
    {
        var response = new AuthResponseDto
        {
            Success = true,
            Message = message,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiry = tokenExpiry,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                OrganizationId = user.OrganizationId,
                OrganizationName = user.Organization?.Name,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                ProfileCompletionPercentage = user.ProfileCompletionPercentage
            }
        };

        return new AuthResult
        {
            IsSuccess = true,
            Data = response,
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

    /// <summary>
    /// Convert to DTO for backward compatibility
    /// </summary>
    public AuthResponseDto ToDto()
    {
        if (Data != null)
        {
            return Data;
        }

        return new AuthResponseDto
        {
            Success = IsSuccess,
            Message = Message,
            Errors = Errors
        };
    }

}