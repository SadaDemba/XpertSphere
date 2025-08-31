using Microsoft.Extensions.Options;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public interface IEntraIdErrorHandler
{
    AuthResult HandleOAuth2Error(string error, string? errorDescription, string authFlow, string? state = null);
    AuthResult HandleApiTimeout(string operation, TimeSpan duration);
    AuthResult HandleRateLimitExceeded(string operation, TimeSpan retryAfter);
    AuthResult HandleServiceUnavailable(string operation, Exception? exception = null);
    bool IsEntraIdAvailable();
    Task<bool> CheckEntraIdHealthAsync();
}

public class EntraIdErrorHandler : IEntraIdErrorHandler
{
    private readonly ILogger<EntraIdErrorHandler> _logger;
    private readonly EntraIdSettings _entraIdSettings;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private static DateTime _lastHealthCheck = DateTime.MinValue;
    private static bool _lastHealthStatus = true;
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(5);

    public EntraIdErrorHandler(
        ILogger<EntraIdErrorHandler> logger,
        IOptions<EntraIdSettings> entraIdSettings,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _entraIdSettings = entraIdSettings.Value;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
    }

    public AuthResult HandleOAuth2Error(string error, string? errorDescription, string authFlow, string? state = null)
    {
        var errorCode = NormalizeErrorCode(error);
        var severity = GetErrorSeverity(errorCode);
        
        _logger.Log(severity, 
            "OAuth2 error in {AuthFlow}: {Error} - {Description}. State: {State}", 
            authFlow, error, errorDescription, state);

        var userMessage = GetUserFriendlyMessage(errorCode, authFlow);
        var fallbackAction = GetFallbackAction(errorCode, authFlow);

        return AuthResult.Failure(userMessage, [errorDescription ?? error])
            .WithStatusCode(GetHttpStatusCode(errorCode))
            .WithMetadata("error_code", errorCode)
            .WithMetadata("auth_flow", authFlow)
            .WithMetadata("fallback_action", fallbackAction);
    }

    public AuthResult HandleApiTimeout(string operation, TimeSpan duration)
    {
        _logger.LogWarning(
            "Entra ID API timeout for operation {Operation} after {Duration}ms", 
            operation, duration.TotalMilliseconds);

        if (duration > TimeSpan.FromSeconds(30))
        {
            _logger.LogError(
                "Critical timeout for {Operation}. Consider fallback to local auth", 
                operation);
        }

        return AuthResult.Failure(
            "Authentication service is experiencing delays. Please try again or use local authentication.",
            ["API_TIMEOUT"]
        ).WithStatusCode(504);
    }

    public AuthResult HandleRateLimitExceeded(string operation, TimeSpan retryAfter)
    {
        _logger.LogWarning(
            "Rate limit exceeded for {Operation}. Retry after {RetryAfter} seconds", 
            operation, retryAfter.TotalSeconds);

        return AuthResult.Failure(
            $"Too many authentication requests. Please wait {Math.Ceiling(retryAfter.TotalSeconds)} seconds before trying again.",
            ["RATE_LIMIT_EXCEEDED"]
            )
            .WithStatusCode(429)
            .WithMetadata("retry_after", retryAfter.TotalSeconds);
    }

    public AuthResult HandleServiceUnavailable(string operation, Exception? exception = null)
    {
        _logger.LogError(exception,
            "Entra ID service unavailable for operation {Operation}", operation);

        var fallbackMessage = _environment.IsDevelopment() 
            ? "Entra ID is unavailable in development. Use local authentication." 
            : "External authentication is temporarily unavailable. Please use local authentication or try again later.";

        return AuthResult.Failure(fallbackMessage, ["SERVICE_UNAVAILABLE"])
            .WithStatusCode(503)
            .WithMetadata("fallback_available", true)
            .WithMetadata("local_auth_endpoint", "/api/auth/login");
    }

    public bool IsEntraIdAvailable()
    {
        if (_environment.IsDevelopment())
        {
            return false;
        }

        if (DateTime.UtcNow - _lastHealthCheck < HealthCheckInterval)
        {
            return _lastHealthStatus;
        }

        // Trigger async health check but return last known status
        _ = Task.Run(async () => await CheckEntraIdHealthAsync());
        return _lastHealthStatus;
    }

    public async Task<bool> CheckEntraIdHealthAsync()
    {
        if (_environment.IsDevelopment())
        {
            _lastHealthStatus = false;
            _lastHealthCheck = DateTime.UtcNow;
            return false;
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var healthEndpoint = $"{_entraIdSettings.B2B.Authority}/.well-known/openid_configuration";
            var response = await httpClient.GetAsync(healthEndpoint);
            
            _lastHealthStatus = response.IsSuccessStatusCode;
            _lastHealthCheck = DateTime.UtcNow;

            _logger.LogInformation(
                "Entra ID health check: {Status} ({StatusCode})", 
                _lastHealthStatus ? "Healthy" : "Unhealthy", 
                response.StatusCode);

            return _lastHealthStatus;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entra ID health check failed");
            _lastHealthStatus = false;
            _lastHealthCheck = DateTime.UtcNow;
            return false;
        }
    }

    private static string NormalizeErrorCode(string error)
    {
        return error.ToUpperInvariant() switch
        {
            "ACCESS_DENIED" => "ACCESS_DENIED",
            "INVALID_REQUEST" => "INVALID_REQUEST",
            "INVALID_CLIENT" => "INVALID_CLIENT",
            "INVALID_GRANT" => "INVALID_GRANT",
            "UNAUTHORIZED_CLIENT" => "UNAUTHORIZED_CLIENT",
            "UNSUPPORTED_GRANT_TYPE" => "UNSUPPORTED_GRANT_TYPE",
            "INVALID_SCOPE" => "INVALID_SCOPE",
            "SERVER_ERROR" => "SERVER_ERROR",
            "TEMPORARILY_UNAVAILABLE" => "TEMPORARILY_UNAVAILABLE",
            _ => "UNKNOWN_ERROR"
        };
    }

    private static LogLevel GetErrorSeverity(string errorCode)
    {
        return errorCode switch
        {
            "ACCESS_DENIED" => LogLevel.Information,
            "INVALID_REQUEST" => LogLevel.Warning,
            "INVALID_CLIENT" or "UNAUTHORIZED_CLIENT" => LogLevel.Error,
            "SERVER_ERROR" or "TEMPORARILY_UNAVAILABLE" => LogLevel.Error,
            _ => LogLevel.Warning
        };
    }

    private static string GetUserFriendlyMessage(string errorCode, string authFlow)
    {
        return errorCode switch
        {
            "ACCESS_DENIED" => "Authentication was cancelled or access was denied.",
            "INVALID_REQUEST" => "Invalid authentication request. Please try again.",
            "INVALID_CLIENT" => "Authentication configuration error. Please contact support.",
            "INVALID_GRANT" => "Authentication session expired. Please try again.",
            "UNAUTHORIZED_CLIENT" => "This application is not authorized for this authentication method.",
            "UNSUPPORTED_GRANT_TYPE" => "Authentication method not supported.",
            "INVALID_SCOPE" => "Insufficient permissions for this authentication.",
            "SERVER_ERROR" => "Authentication service error. Please try again or use local authentication.",
            "TEMPORARILY_UNAVAILABLE" => "Authentication service is temporarily unavailable. Please try again later.",
            _ => "Authentication failed. Please try again or use local authentication."
        };
    }

    private static string GetFallbackAction(string errorCode, string authFlow)
    {
        return errorCode switch
        {
            "ACCESS_DENIED" => "retry_or_local",
            "INVALID_REQUEST" => "retry",
            "INVALID_CLIENT" or "UNAUTHORIZED_CLIENT" => "contact_support",
            "SERVER_ERROR" or "TEMPORARILY_UNAVAILABLE" => "use_local",
            _ => "retry_or_local"
        };
    }

    private static int GetHttpStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "ACCESS_DENIED" => 403,
            "INVALID_REQUEST" => 400,
            "INVALID_CLIENT" or "UNAUTHORIZED_CLIENT" => 401,
            "INVALID_GRANT" => 401,
            "UNSUPPORTED_GRANT_TYPE" => 400,
            "INVALID_SCOPE" => 400,
            "SERVER_ERROR" => 500,
            "TEMPORARILY_UNAVAILABLE" => 503,
            _ => 400
        };
    }
}

