using System.Security.Claims;
using Microsoft.Extensions.Options;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Services;

public class AuthenticationLogger : IAuthenticationLogger
{
    private readonly ILogger<AuthenticationLogger> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationLogger(
        ILogger<AuthenticationLogger> logger,
        IWebHostEnvironment environment,
        IOptions<JwtSettings> jwtSettings)
    {
        _logger = logger;
        _environment = environment;
        _jwtSettings = jwtSettings.Value;
    }

    public void LogAuthenticationAttempt(string email, string authType, string source, string? userAgent = null)
    {
        var sanitizedEmail = SanitizeEmail(email);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AuthenticationAttempt",
            ["Email"] = sanitizedEmail,
            ["AuthType"] = authType,
            ["Source"] = source,
            ["Timestamp"] = DateTime.UtcNow,
            ["Environment"] = _environment.EnvironmentName
        });

        _logger.LogInformation(
            "Authentication attempt: {Email} using {AuthType} from {Source}",
            sanitizedEmail, authType, source);

        if (!string.IsNullOrEmpty(userAgent))
        {
            _logger.LogDebug("User-Agent: {UserAgent}", userAgent);
        }
    }

    public void LogAuthenticationSuccess(string email, string authType, string userId, TimeSpan duration)
    {
        var sanitizedEmail = SanitizeEmail(email);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AuthenticationSuccess",
            ["Email"] = sanitizedEmail,
            ["AuthType"] = authType,
            ["UserId"] = userId,
            ["Duration"] = duration.TotalMilliseconds,
            ["Timestamp"] = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Authentication successful: {Email} using {AuthType} (UserId: {UserId}) in {Duration}ms",
            sanitizedEmail, authType, userId, duration.TotalMilliseconds);

        // Log performance metrics
        if (duration > TimeSpan.FromSeconds(5))
        {
            _logger.LogWarning(
                "Slow authentication: {AuthType} took {Duration}ms for {Email}",
                authType, duration.TotalMilliseconds, sanitizedEmail);
        }
    }

    public void LogAuthenticationFailure(string email, string authType, string reason, string? errorCode = null)
    {
        var sanitizedEmail = SanitizeEmail(email);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AuthenticationFailure",
            ["Email"] = sanitizedEmail,
            ["AuthType"] = authType,
            ["Reason"] = reason,
            ["ErrorCode"] = errorCode ?? "UNKNOWN",
            ["Timestamp"] = DateTime.UtcNow
        });

        _logger.LogWarning(
            "Authentication failed: {Email} using {AuthType} - {Reason} (Code: {ErrorCode})",
            sanitizedEmail, authType, reason, errorCode ?? "UNKNOWN");
    }

    public void LogEntraIdCallback(string state, string authFlow, bool success, string? error = null)
    {
        var stateInfo = ParseStateParameter(state);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "EntraIdCallback",
            ["AuthFlow"] = authFlow,
            ["Success"] = success,
            ["ReturnUrl"] = stateInfo.ReturnUrl,
            ["Error"] = error ?? "None",
            ["Timestamp"] = DateTime.UtcNow
        });

        if (success)
        {
            _logger.LogInformation(
                "Entra ID callback successful: {AuthFlow} flow, returning to {ReturnUrl}",
                authFlow, stateInfo.ReturnUrl);
        }
        else
        {
            _logger.LogWarning(
                "Entra ID callback failed: {AuthFlow} flow - {Error}",
                authFlow, error ?? "Unknown error");
        }
    }

    public void LogAccountLinking(string userId, string email, string externalId, bool success)
    {
        var sanitizedEmail = SanitizeEmail(email);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AccountLinking",
            ["UserId"] = userId,
            ["Email"] = sanitizedEmail,
            ["ExternalId"] = externalId,
            ["Success"] = success,
            ["Timestamp"] = DateTime.UtcNow
        });

        if (success)
        {
            _logger.LogInformation(
                "Account linking successful: User {UserId} linked to Entra ID {ExternalId}",
                userId, externalId);
        }
        else
        {
            _logger.LogWarning(
                "Account linking failed: User {UserId} could not be linked to Entra ID {ExternalId}",
                userId, externalId);
        }
    }

    public void LogFallbackUsage(string originalAuthType, string fallbackAuthType, string reason)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "AuthenticationFallback",
            ["OriginalAuthType"] = originalAuthType,
            ["FallbackAuthType"] = fallbackAuthType,
            ["Reason"] = reason,
            ["Timestamp"] = DateTime.UtcNow
        });

        _logger.LogWarning(
            "Authentication fallback: {OriginalAuthType} -> {FallbackAuthType} due to {Reason}",
            originalAuthType, fallbackAuthType, reason);
    }

    public void LogSecurityEvent(string eventType, string details, string? userId = null, string? email = null)
    {
        var sanitizedEmail = string.IsNullOrEmpty(email) ? null : SanitizeEmail(email);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventType"] = "SecurityEvent",
            ["SecurityEventType"] = eventType,
            ["UserId"] = userId ?? "Unknown",
            ["Email"] = sanitizedEmail ?? "Unknown",
            ["Timestamp"] = DateTime.UtcNow
        });

        _logger.LogWarning(
            "Security event: {EventType} - {Details} (User: {UserId}, Email: {Email})",
            eventType, details, userId ?? "Unknown", sanitizedEmail ?? "Unknown");
    }

    private string SanitizeEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return "Unknown";

        // In production, consider hashing or masking email for privacy
        if (_environment.IsProduction())
        {
            var parts = email.Split('@');
            if (parts.Length == 2)
            {
                var localPart = parts[0];
                var domain = parts[1];
                var maskedLocal = localPart.Length > 3 
                    ? $"{localPart[..2]}***{localPart[^1]}" 
                    : "***";
                return $"{maskedLocal}@{domain}";
            }
        }

        return email;
    }

    private (string ReturnUrl, string AuthType) ParseStateParameter(string state)
    {
        try
        {
            var stateJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var stateData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(stateJson);
            
            var returnUrl = stateData?.TryGetValue("returnUrl", out var returnUrlValue) == true 
                ? returnUrlValue?.ToString() ?? "/" 
                : "/";
                
            var authType = stateData?.TryGetValue("authType", out var authTypeValue) == true 
                ? authTypeValue?.ToString() ?? "Unknown" 
                : "Unknown";
                
            return (returnUrl, authType);
        }
        catch
        {
            return ("/", "Unknown");
        }
    }
}

public static class AuthenticationLoggerExtensions
{
    public static IServiceCollection AddAuthenticationLogging(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationLogger, AuthenticationLogger>();
        return services;
    }
}