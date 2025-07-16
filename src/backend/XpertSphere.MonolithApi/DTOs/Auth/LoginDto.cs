using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.DTOs.Auth;

public record LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    public bool RememberMe { get; init; } = false;
}

public record RegisterDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; init; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [Required]
    public UserType UserType { get; init; }

    public Guid? OrganizationId { get; init; }

    [Phone]
    public string? PhoneNumber { get; init; }

    public bool AcceptTerms { get; init; } = false;

    public bool AcceptPrivacyPolicy { get; init; } = false;
}

public record RefreshTokenDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string RefreshToken { get; init; }
}

public record ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Token { get; init; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; init; }

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; init; }
}

public record ChangePasswordDto
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; init; }

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; init; }
}

public record ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}

public record ConfirmEmailDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Token { get; init; }
}

// Response DTOs
public record AuthResponseDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? TokenExpiry { get; init; }
    public UserDto? User { get; init; }
    public string? EmailConfirmationToken { get; init; }
    public List<string> Errors { get; init; } = [];
}

// Auth Result for service layer
public class AuthResult
{
    public bool IsSuccessful { get; set; }
    public string Message { get; set; } = string.Empty;
    public XpertSphere.MonolithApi.Models.User? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public List<string> Errors { get; set; } = [];

    public static AuthResult Success(string message = "Operation successful")
    {
        return new AuthResult
        {
            IsSuccessful = true,
            Message = message
        };
    }

    public static AuthResult SuccessWithUser(XpertSphere.MonolithApi.Models.User? user, string message = "Operation successful", string? token = null)
    {
        return new AuthResult
        {
            IsSuccessful = true,
            Message = message,
            User = user,
            EmailConfirmationToken = token
        };
    }

    public static AuthResult SuccessWithTokens(XpertSphere.MonolithApi.Models.User? user, string accessToken, string refreshToken, DateTime tokenExpiry, string message = "Login successful")
    {
        return new AuthResult
        {
            IsSuccessful = true,
            Message = message,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiry = tokenExpiry
        };
    }

    public static AuthResult Failure(string message, List<string>? errors = null)
    {
        return new AuthResult
        {
            IsSuccessful = false,
            Message = message,
            Errors = errors ?? []
        };
    }

    public AuthResponseDto ToDto()
    {
        return new AuthResponseDto
        {
            Success = IsSuccessful,
            Message = Message,
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            TokenExpiry = TokenExpiry,
            EmailConfirmationToken = EmailConfirmationToken,
            User = User != null ? new UserDto
            {
                Id = User.Id,
                Email = User.Email!,
                FirstName = User.FirstName,
                LastName = User.LastName,
                FullName = User.FullName,
                UserType = User.UserType,
                OrganizationId = User.OrganizationId,
                OrganizationName = User.Organization?.Name,
                IsActive = User.IsActive,
                EmailConfirmed = User.EmailConfirmed,
                LastLoginAt = User.LastLoginAt,
                ProfileCompletionPercentage = User.ProfileCompletionPercentage
            } : null,
            Errors = Errors
        };
    }
}