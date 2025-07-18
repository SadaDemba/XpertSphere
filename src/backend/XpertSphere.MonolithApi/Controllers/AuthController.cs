using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.Interfaces;
using System.Security.Claims;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.DTOs.User;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            
            if (!registerDto.AcceptTerms || !registerDto.AcceptPrivacyPolicy)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "You must accept the terms of service and privacy policy to register",
                    Errors = ["Terms and privacy policy acceptance required"]
                });
            }

            var result = await _authService.RegisterAsync(registerDto);
            var response = result.ToDto();

            if (result.IsSuccessful)
            {
                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                return CreatedAtAction(nameof(Register), response);
            }

            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred during registration",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);
            var response = result.ToDto();

            if (result.IsSuccessful)
            {
                _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
                return Ok(response);
            }

            return Unauthorized(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred during login",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            var response = result.ToDto();

            if (result.IsSuccessful)
            {
                return Ok(response);
            }

            return Unauthorized(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred during token refresh",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> Logout()
    {
        try
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
            var response = result.ToDto();

            if (result.IsSuccessful)
            {
                _logger.LogInformation("User logged out successfully: {Email}", email);
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user logout");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred during logout",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ConfirmEmailAsync(confirmEmailDto.Email, confirmEmailDto.Token);
            var response = result.ToDto();

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Email confirmed successfully: {Email}", confirmEmailDto.Email);
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email confirmation");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred during email confirmation",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _authService.ForgotPasswordAsync(forgotPasswordDto.Email);

            // Always return success to prevent email enumeration attacks
            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred while processing your request",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            var response = result.ToDto();

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Password reset successfully: {Email}", resetPasswordDto.Email);
                return Ok(response);
            }

            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, new AuthResponseDto
            {
                Success = false,
                Message = "An internal error occurred during password reset",
                Errors = ["Internal server error"]
            });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<UserDto> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var name = User.FindFirstValue(ClaimTypes.Name);
            var userType = User.FindFirstValue("UserType");
            var organizationId = User.FindFirstValue("OrganizationId");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { message = "Invalid user context" });
            }

            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var currentUser = new UserDto
            {
                Id = Guid.Parse(userId),
                Email = email,
                FirstName = name?.Split(' ').FirstOrDefault() ?? "",
                LastName = name?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                FullName = name ?? "",
                UserType = Enum.TryParse<UserType>(userType, out var parsedUserType) ? parsedUserType : UserType.Candidate,
                OrganizationId = Guid.TryParse(organizationId, out var orgId) ? orgId : null,
                IsActive = true,
                EmailConfirmed = true,
                Roles = roles
            };

            return Ok(currentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user information");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public ActionResult GetAuthStatus()
    {
        return Ok(new
        {
            authenticated = true,
            timestamp = DateTime.UtcNow,
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });
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