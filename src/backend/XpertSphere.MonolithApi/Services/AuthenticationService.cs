using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Services;

public class AuthenticationService : Interfaces.IAuthenticationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return AuthResult.Failure("User with this email already exists");
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                UserType = registerDto.UserType,
                OrganizationId = registerDto.OrganizationId,
                PhoneNumber = registerDto.PhoneNumber,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };


            user.CalculateProfileCompletion();

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return AuthResult.Failure($"Registration failed: {errors}");
            }

            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

            // Generate email confirmation token
            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            return AuthResult.SuccessWithUser(user, "Registration successful. Please check your email to confirm your account.", emailConfirmationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration for {Email}", registerDto.Email);
            return AuthResult.Failure("An error occurred during registration");
        }
    }

    public async Task<AuthResult> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                return AuthResult.Failure("Invalid credentials");
            }

            if (user.IsAccountLocked)
            {
                _logger.LogWarning("Login attempt on locked account: {Email}", loginDto.Email);
                return AuthResult.Failure($"Account is locked until {user.AccountLockedUntil:yyyy-MM-dd HH:mm}");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt on inactive account: {Email}", loginDto.Email);
                return AuthResult.Failure("Account is inactive");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Reset failed login attempts and update last login
                user.UpdateLastLogin();
                await _userManager.UpdateAsync(user);

                // Generate tokens
                var accessToken = await GenerateAccessTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                // Save refresh token
                user.SetRefreshToken(refreshToken, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

                return AuthResult.SuccessWithTokens(
                    user,
                    accessToken,
                    refreshToken,
                    DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                    "Login successful"
                );
            }

            if (result.IsLockedOut)
            {
                user.IncrementFailedLogin();
                await _userManager.UpdateAsync(user);

                _logger.LogWarning("Account locked out for user: {Email}", loginDto.Email);
                return AuthResult.Failure("Account locked due to multiple failed login attempts");
            }

            if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login not allowed for user: {Email}", loginDto.Email);
                return AuthResult.Failure("Login not allowed. Please confirm your email address");
            }

            // Increment failed login attempts
            user.IncrementFailedLogin();
            await _userManager.UpdateAsync(user);

            _logger.LogWarning("Failed login attempt for user: {Email}", loginDto.Email);
            return AuthResult.Failure("Invalid credentials");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for {Email}", loginDto.Email);
            return AuthResult.Failure("An error occurred during login");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(refreshTokenDto.Email);
            if (user == null || !user.IsTokenValid || user.RefreshToken != refreshTokenDto.RefreshToken)
            {
                _logger.LogWarning("Invalid refresh token attempt for {Email}", refreshTokenDto.Email);
                return AuthResult.Failure("Invalid refresh token");
            }

            // Generate new tokens
            var accessToken = await GenerateAccessTokenAsync(user);
            var newRefreshToken = GenerateRefreshToken();

            // Update refresh token
            user.SetRefreshToken(newRefreshToken, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Tokens refreshed for user: {Email}", refreshTokenDto.Email);

            return AuthResult.SuccessWithTokens(
                user,
                accessToken,
                newRefreshToken,
                DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                "Token refreshed successfully"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token refresh for {Email}", refreshTokenDto.Email);
            return AuthResult.Failure("An error occurred during token refresh");
        }
    }

    public async Task<AuthResult> LogoutAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return AuthResult.Failure("User not found");
            }

            // Clear refresh token
            user.ClearRefreshToken();
            await _userManager.UpdateAsync(user);

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {Email} logged out successfully", email);
            return AuthResult.Success("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout for {Email}", email);
            return AuthResult.Failure("An error occurred during logout");
        }
    }

    public async Task<AuthResult> ConfirmEmailAsync(string email, string token)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return AuthResult.Failure("User not found");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for user: {Email}", email);
                return AuthResult.Success("Email confirmed successfully");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AuthResult.Failure($"Email confirmation failed: {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during email confirmation for {Email}", email);
            return AuthResult.Failure("An error occurred during email confirmation");
        }
    }

    public async Task<AuthResult> ForgotPasswordAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return AuthResult.Success("If an account with that email exists, a password reset link has been sent");
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            user.SetPasswordResetToken(resetToken, TimeSpan.FromHours(1));
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password reset token generated for user: {Email}", email);

            return AuthResult.SuccessWithUser(user, "Password reset email sent", resetToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset request for {Email}", email);
            return AuthResult.Failure("An error occurred while processing your request");
        }
    }

    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return AuthResult.Failure("Invalid request");
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (result.Succeeded)
            {
                user.ClearPasswordResetToken();
                user.LastPasswordChangeAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password reset successfully for user: {Email}", resetPasswordDto.Email);
                return AuthResult.Success("Password reset successful");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AuthResult.Failure($"Password reset failed: {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset for {Email}", resetPasswordDto.Email);
            return AuthResult.Failure("An error occurred during password reset");
        }
    }

    private async Task<string> GenerateAccessTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("UserType", user.UserType.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add organization claim if user belongs to one
        if (user.OrganizationId.HasValue)
        {
            claims.Add(new Claim("OrganizationId", user.OrganizationId.Value.ToString()));
        }

        // Add role claims
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}