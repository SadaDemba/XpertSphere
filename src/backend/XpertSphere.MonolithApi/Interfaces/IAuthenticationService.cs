using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(RegisterDto registerDto);
    Task<AuthResult> LoginAsync(LoginDto loginDto);
    Task<AuthResult> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<AuthResult> LogoutAsync(string email);
    Task<AuthResult> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
    Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto  forgotPasswordDto);
    Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId);
}