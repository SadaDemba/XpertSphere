using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account (restricted to authorized users only)
    /// </summary>
    [HttpPost("register")]
    [Authorize(Policy = "CanCreateUsers")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Register a new candidate with complete profile and optional resume
    /// </summary>
    [HttpPost("register/candidate")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RegisterCandidate([FromForm] RegisterCandidateDto registerDto, IFormFile? resume = null)
    {
        var result = await _authService.RegisterCandidateAsync(registerDto, resume);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> Logout()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Invalid user context",
                Errors = ["User email not found in token"]
            });
        }

        var result = await _authService.LogoutAsync(email);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
    {
        var result = await _authService.ConfirmEmailAsync(confirmEmailDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var result = await _authService.ResetPasswordAsync(resetPasswordDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Admin reset password for a user by email (SuperAdmin XpertSphere for all, Admin for their organization)
    /// </summary>
    [HttpPost("admin-reset-password")]
    [Authorize(Policy = "CanResetPasswords")]
    public async Task<ActionResult<AuthResponseDto>> AdminResetPassword([FromBody] AdminResetPasswordDto adminResetPasswordDto)
    {
        var result = await _authService.AdminResetPasswordAsync(adminResetPasswordDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Generate Entra ID login URL based on user type
    /// </summary>
    /// <param name="email">User email to determine organization membership</param>
    /// <param name="returnUrl">URL to redirect after authentication</param>
    /// <returns>Entra ID login URL or local login instruction</returns>
    [HttpGet("login-url")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthUrlResponseDto), 200)]
    public ActionResult<AuthUrlResponseDto> GetLoginUrl([FromQuery] string? email, [FromQuery] string? returnUrl = "/dashboard")
    {
        var result = _authService.GetLoginUrl(email, returnUrl);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Generate Entra ID signup URL for candidates
    /// </summary>
    /// <param name="returnUrl">URL to redirect after registration</param>
    /// <returns>Entra ID B2C signup URL or local registration instruction</returns>
    [HttpGet("signup-url")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthUrlResponseDto), 200)]
    public ActionResult<AuthUrlResponseDto> GetSignupUrl([FromQuery] string? returnUrl = "/profile")
    {
        var result = _authService.GetSignupUrl(returnUrl);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Link Entra ID account to existing user
    /// </summary>
    /// <param name="linkAccountDto">Account linking data</param>
    /// <returns>Link result</returns>
    [HttpPost("link-account")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AuthResponseDto>> LinkAccount([FromBody] LinkAccountDto linkAccountDto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new AuthResponseDto
            {
                Success = false,
                Message = "Invalid user context",
                Errors = ["User not authenticated"]
            });
        }

        var result = await _authService.LinkEntraIdAccount(userId.Value.ToString(), linkAccountDto.EntraIdToken);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Determine user type for appropriate authentication flow
    /// </summary>
    /// <param name="email">User email address</param>
    /// <returns>User type and recommended authentication method</returns>
    [HttpGet("user-type")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserTypeResponseDto), 200)]
    public async Task<ActionResult<UserTypeResponseDto>> GetUserType([FromQuery] string email)
    {
        var result = await _authService.GetUserTypeAsync(email);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get current user information with Entra ID claims support
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Invalid user context" });
        }

        var result = await _authService.GetCurrentUserAsync(userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Authentication Service"
        });
    }
}