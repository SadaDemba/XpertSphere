using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthCallbackController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthCallbackController> _logger;
    private readonly IEntraIdErrorHandler _errorHandler;
    private readonly IAuthenticationLogger _authLogger;

    public AuthCallbackController(
        IAuthenticationService authenticationService,
        ILogger<AuthCallbackController> logger,
        IEntraIdErrorHandler errorHandler,
        IAuthenticationLogger authLogger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
        _errorHandler = errorHandler;
        _authLogger = authLogger;
    }

    /// <summary>
    /// Handle OAuth2 callback for Entra ID B2B authentication (internal users)
    /// </summary>
    /// <param name="code">Authorization code from Entra ID</param>
    /// <param name="state">State parameter for security and return URL</param>
    /// <param name="error">Error code if authentication failed</param>
    /// <param name="error_description">Error description if authentication failed</param>
    /// <returns>Redirect to frontend with authentication result</returns>
    [HttpGet("b2b")]
    [ProducesResponseType(302)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> HandleB2BCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        try
        {
            _authLogger.LogEntraIdCallback(state ?? "unknown", "B2B", false, error);
            
            if (!string.IsNullOrEmpty(error))
            {
                var errorResult = _errorHandler.HandleOAuth2Error(error, error_description, "B2B", state);
                var errorCode = errorResult.GetMetadata<string>("error_code") ?? error;
                return RedirectToClientWithError(errorCode, error_description, "b2b");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                var errorResult = _errorHandler.HandleOAuth2Error("invalid_request", "Missing required parameters", "B2B", state);
                return RedirectToClientWithError("invalid_request", "Missing required parameters", "b2b");
            }

            var result = await _authenticationService.HandleEntraIdCallback(code, state, error);
            
            if (result.IsSuccess)
            {
                _authLogger.LogEntraIdCallback(state, "B2B", true);
                var returnUrl = result.Data?.RedirectUrl ?? "/dashboard";
                
                if (!string.IsNullOrEmpty(result.Data?.AccessToken))
                {
                    var redirectUrl = BuildClientRedirectUrl(returnUrl, new
                    {
                        access_token = result.Data.AccessToken,
                        refresh_token = result.Data.RefreshToken,
                        expires_at = result.Data.TokenExpiry?.ToString("O"),
                        user_id = result.Data.User?.Id.ToString(),
                        message = result.Message
                    });
                    
                    _logger.LogInformation("B2B authentication successful for user {UserId}", result.Data.User?.Id);
                    return Redirect(redirectUrl);
                }
                
                var simpleRedirectUrl = BuildClientRedirectUrl(returnUrl, new { message = result.Message });
                return Redirect(simpleRedirectUrl);
            }

            _authLogger.LogEntraIdCallback(state, "B2B", false, result.Message);
            return RedirectToClientWithError("authentication_failed", result.Message, "b2b");
        }
        catch (Exception ex)
        {
            _authLogger.LogSecurityEvent("B2B_CALLBACK_ERROR", ex.Message, null, null);
            var errorResult = _errorHandler.HandleServiceUnavailable("B2B callback", ex);
            return RedirectToClientWithError("server_error", "An unexpected error occurred", "b2b");
        }
    }

    /// <summary>
    /// Handle OAuth2 callback for Entra ID B2C authentication (candidates)
    /// </summary>
    /// <param name="code">Authorization code from Entra ID</param>
    /// <param name="state">State parameter for security and return URL</param>
    /// <param name="error">Error code if authentication failed</param>
    /// <param name="error_description">Error description if authentication failed</param>
    /// <returns>Redirect to frontend with an authentication result</returns>
    [HttpGet("b2c")]
    [ProducesResponseType(302)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> HandleB2CCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        try
        {
            _authLogger.LogEntraIdCallback(state ?? "unknown", "B2C", false, error);
            
            if (!string.IsNullOrEmpty(error))
            {
                var errorResult = _errorHandler.HandleOAuth2Error(error, error_description, "B2C", state);
                var errorCode = errorResult.GetMetadata<string>("error_code") ?? error;
                return RedirectToClientWithError(errorCode, error_description, "b2c");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                var errorResult = _errorHandler.HandleOAuth2Error("invalid_request", "Missing required parameters", "B2C", state);
                return RedirectToClientWithError("invalid_request", "Missing required parameters", "b2c");
            }

            var result = await _authenticationService.HandleEntraIdCallback(code, state, error);
            
            if (result.IsSuccess)
            {
                var returnUrl = result.Data?.RedirectUrl ?? "/profile";
                
                if (!string.IsNullOrEmpty(result.Data?.AccessToken))
                {
                    var redirectUrl = BuildClientRedirectUrl(returnUrl, new
                    {
                        access_token = result.Data.AccessToken,
                        refresh_token = result.Data.RefreshToken,
                        expires_at = result.Data.TokenExpiry?.ToString("O"),
                        user_id = result.Data.User?.Id.ToString(),
                        message = result.Message
                    });
                    
                    _logger.LogInformation("B2C authentication successful for user {UserId}", result.Data.User?.Id);
                    return Redirect(redirectUrl);
                }
                
                var simpleRedirectUrl = BuildClientRedirectUrl(returnUrl, new { message = result.Message });
                return Redirect(simpleRedirectUrl);
            }

            _logger.LogWarning("B2C authentication failed: {Message}", result.Message);
            return RedirectToClientWithError("authentication_failed", result.Message, "b2c");
        }
        catch (Exception ex)
        {
            _authLogger.LogSecurityEvent("B2C_CALLBACK_ERROR", ex.Message, null, null);
            var errorResult = _errorHandler.HandleServiceUnavailable("B2C callback", ex);
            return RedirectToClientWithError("server_error", "An unexpected error occurred", "b2c");
        }
    }

    /// <summary>
    /// Handle authentication errors from Entra ID
    /// </summary>
    /// <param name="errorRequest">Error details from the authentication flow</param>
    /// <returns>Error response with details</returns>
    [HttpPost("error")]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 500)]
    public ActionResult<object> HandleAuthenticationError([FromBody] AuthErrorRequest errorRequest)
    {
        try
        {
            _logger.LogWarning("Authentication error received: {Error} - {Description} - Flow: {Flow}", 
                errorRequest.Error, errorRequest.ErrorDescription, errorRequest.AuthFlow);

            var errorResponse = new
            {
                error = errorRequest.Error ?? "unknown_error",
                error_description = errorRequest.ErrorDescription ?? "An unknown error occurred",
                auth_flow = errorRequest.AuthFlow ?? "unknown",
                timestamp = DateTime.UtcNow,
                support_message = "Please try again or contact support if the problem persists"
            };

            var statusCode = errorRequest.Error switch
            {
                "access_denied" => 403,
                "invalid_request" => 400,
                "invalid_client" => 401,
                "invalid_grant" => 401,
                "unauthorized_client" => 401,
                "unsupported_grant_type" => 400,
                "invalid_scope" => 400,
                "server_error" => 500,
                "temporarily_unavailable" => 503,
                _ => 400
            };

            return StatusCode(statusCode, errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling authentication error request");
            return StatusCode(500, new
            {
                error = "server_error",
                error_description = "An unexpected error occurred while processing the error",
                timestamp = DateTime.UtcNow
            });
        }
    }

    #region Private Helper Methods

    private ActionResult RedirectToClientWithError(string error, string? description, string authFlow)
    {
        var errorUrl = BuildClientRedirectUrl("/auth/error", new
        {
            error,
            error_description = description,
            auth_flow = authFlow,
            timestamp = DateTime.UtcNow.ToString("O")
        });

        return Redirect(errorUrl);
    }

    private string BuildClientRedirectUrl(string basePath, object? parameters = null)
    {
        var clientBaseUrl = GetClientBaseUrl();
        var url = $"{clientBaseUrl}{basePath}";

        if (parameters != null)
        {
            var queryParams = new List<string>();
            var properties = parameters.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(parameters)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    queryParams.Add($"{prop.Name}={Uri.EscapeDataString(value)}");
                }
            }

            if (queryParams.Count > 0)
            {
                url += $"?{string.Join("&", queryParams)}";
            }
        }

        return url;
    }

    private string GetClientBaseUrl()
    {
        return HttpContext.Request.Headers.Origin.FirstOrDefault() ?? 
               Environment.GetEnvironmentVariable("CLIENT_BASE_URL") ?? 
               "http://localhost:3000";
    }

    #endregion
}

public class AuthErrorRequest
{
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string? AuthFlow { get; set; }
    public string? ReturnUrl { get; set; }
    public DateTime? Timestamp { get; set; }
}