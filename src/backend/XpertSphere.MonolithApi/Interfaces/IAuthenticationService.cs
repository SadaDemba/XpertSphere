using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

/// <summary>
/// Service interface for user authentication and authorization operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Register a new user with basic information
    /// </summary>
    Task<AuthResult> RegisterAsync(RegisterDto registerDto);
    
    /// <summary>
    /// Register a new candidate with complete profile information
    /// </summary>
    Task<AuthResult> RegisterCandidateAsync(RegisterCandidateDto registerDto, IFormFile? resumeFile = null);
    
    /// <summary>
    /// Authenticate a user with email and password
    /// </summary>
    Task<AuthResult> LoginAsync(LoginDto loginDto);
    
    /// <summary>
    /// Refresh an expired access token using a refresh token
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    
    /// <summary>
    /// Sign out a user and invalidate their tokens
    /// </summary>
    Task<AuthResult> LogoutAsync(string email);
    
    /// <summary>
    /// Confirm a user's email address using a confirmation token
    /// </summary>
    Task<AuthResult> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
    
    /// <summary>
    /// Initiate password reset process by sending reset email
    /// </summary>
    Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    
    /// <summary>
    /// Reset a user's password using a reset token
    /// </summary>
    Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    
    /// <summary>
    /// Admin-initiated password reset for a user
    /// </summary>
    Task<AuthResult> AdminResetPasswordAsync(AdminResetPasswordDto adminResetPasswordDto);
    
    /// <summary>
    /// Get the current authenticated user's information
    /// </summary>
    Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId);
    
    /// <summary>
    /// Handle callback from Entra ID authentication
    /// </summary>
    Task<AuthResult> HandleEntraIdCallback(string code, string state, string? error = null);
    
    /// <summary>
    /// Link an existing user account with Entra ID
    /// </summary>
    Task<AuthResult> LinkEntraIdAccount(string userId, string entraIdToken);
    
    /// <summary>
    /// Generate login URL for authentication
    /// </summary>
    ServiceResult<AuthUrlResponseDto> GetLoginUrl(string? email, string? returnUrl);
    
    /// <summary>
    /// Generate signup URL for user registration
    /// </summary>
    ServiceResult<AuthUrlResponseDto> GetSignupUrl(string? returnUrl);
    
    /// <summary>
    /// Determine user type (organizational or candidate) based on email
    /// </summary>
    Task<ServiceResult<UserTypeResponseDto>> GetUserTypeAsync(string email);
}