using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterDto registerDto);
    Task<AuthResult> LoginAsync(LoginDto loginDto);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<AuthResult> LogoutAsync(string email);
    Task<AuthResult> ConfirmEmailAsync(string email, string token);
    Task<AuthResult> ForgotPasswordAsync(string email);
    Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
}