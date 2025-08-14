using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Models.Base;

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

    [Phone]
    public string? PhoneNumber { get; init; }
    
    [Required]
    public required List<Training>? Trainings { get; init; }
    
    [Required]
    public required List<Experience>? Experiences { get; init; }

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
