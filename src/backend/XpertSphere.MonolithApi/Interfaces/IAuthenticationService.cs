using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterDto registerDto);
    Task<AuthResult> RegisterCandidateAsync(RegisterCandidateDto registerDto, IFormFile? resumeFile = null);
    Task<AuthResult> LoginAsync(LoginDto loginDto);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<AuthResult> LogoutAsync(string email);
    Task<AuthResult> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
    Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto  forgotPasswordDto);
    Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId);
    Task<AuthResult> HandleEntraIdCallback(string code, string state, string? error = null);
    Task<AuthResult> LinkEntraIdAccount(string userId, string entraIdToken);
    ServiceResult<AuthUrlResponseDto> GetLoginUrl(string? email, string? returnUrl);
    ServiceResult<AuthUrlResponseDto> GetSignupUrl(string? returnUrl);
    Task<ServiceResult<UserTypeResponseDto>> GetUserTypeAsync(string email);
}