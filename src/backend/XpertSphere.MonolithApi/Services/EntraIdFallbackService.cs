using Microsoft.Extensions.Options;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public interface IEntraIdFallbackService
{
    Task<AuthResult> HandleAuthenticationWithFallback(LoginDto loginDto, Func<Task<AuthResult>> entraIdAuth, Func<Task<AuthResult>> localAuth);
    Task<AuthResult> HandleRegistrationWithFallback(RegisterDto registerDto, Func<Task<AuthResult>> entraIdReg, Func<Task<AuthResult>> localReg);
   ServiceResult<AuthUrlResponseDto> GetAuthUrlWithFallback(string? email, string? returnUrl, Func<ServiceResult<AuthUrlResponseDto>> entraIdUrl);
    bool ShouldUseEntraId(string? email = null);
    AuthResult CreateFallbackResponse(string operation, string reason, string? redirectEndpoint = null);
}

public class EntraIdFallbackService : IEntraIdFallbackService
{
    private readonly ILogger<EntraIdFallbackService> _logger;
    private readonly IAuthenticationLogger _authLogger;
    private readonly IEntraIdErrorHandler _errorHandler;
    private readonly IWebHostEnvironment _environment;
    private readonly EntraIdSettings _entraIdSettings;
    
    private static readonly Dictionary<string, DateTime> _failureTimestamps = new();
    private static readonly Dictionary<string, int> _failureCounts = new();
    private static readonly TimeSpan CircuitBreakerTimeout = TimeSpan.FromMinutes(10);
    private static readonly int MaxFailures = 3;

    public EntraIdFallbackService(
        ILogger<EntraIdFallbackService> logger,
        IAuthenticationLogger authLogger,
        IEntraIdErrorHandler errorHandler,
        IWebHostEnvironment environment,
        IOptions<EntraIdSettings> entraIdSettings)
    {
        _logger = logger;
        _authLogger = authLogger;
        _errorHandler = errorHandler;
        _environment = environment;
        _entraIdSettings = entraIdSettings.Value;
    }

    public async Task<AuthResult> HandleAuthenticationWithFallback(
        LoginDto loginDto, 
        Func<Task<AuthResult>> entraIdAuth, 
        Func<Task<AuthResult>> localAuth)
    {
        var startTime = DateTime.UtcNow;
        _authLogger.LogAuthenticationAttempt(loginDto.Email, "EntraId_With_Fallback", "Login");

        // Check if user explicitly wants local auth
        if (loginDto.ForceLocalAuth || loginDto.SkipEntraIdRedirect)
        {
            _authLogger.LogFallbackUsage("EntraId", "Local", "User requested local auth");
            return await ExecuteWithLogging(() => localAuth(), "Local", loginDto.Email, startTime);
        }

        // Check if Entra ID is available
        if (!ShouldUseEntraId(loginDto.Email))
        {
            _authLogger.LogFallbackUsage("EntraId", "Local", "Entra ID unavailable");
            return await ExecuteWithLogging(() => localAuth(), "Local", loginDto.Email, startTime);
        }

        try
        {
            var entraResult = await ExecuteWithTimeout(() => entraIdAuth(), TimeSpan.FromSeconds(15));
            
            if (entraResult.IsSuccess)
            {
                ResetFailureCount("authentication");
                return entraResult;
            }

            // If Entra ID fails, check if we should fallback
            if (ShouldFallbackToLocal(entraResult))
            {
                _authLogger.LogFallbackUsage("EntraId", "Local", $"Entra ID failed: {entraResult.Message}");
                return await ExecuteWithLogging(() => localAuth(), "Local", loginDto.Email, startTime);
            }

            return entraResult;
        }
        catch (TimeoutException)
        {
            RecordFailure("authentication");
            _authLogger.LogFallbackUsage("EntraId", "Local", "Entra ID timeout");
            return await ExecuteWithLogging(() => localAuth(), "Local", loginDto.Email, startTime);
        }
        catch (Exception ex)
        {
            RecordFailure("authentication");
            _logger.LogError(ex, "Unexpected error during Entra ID authentication for {Email}", loginDto.Email);
            _authLogger.LogFallbackUsage("EntraId", "Local", $"Unexpected error: {ex.Message}");
            return await ExecuteWithLogging(() => localAuth(), "Local", loginDto.Email, startTime);
        }
    }

    public async Task<AuthResult> HandleRegistrationWithFallback(
        RegisterDto registerDto, 
        Func<Task<AuthResult>> entraIdReg, 
        Func<Task<AuthResult>> localReg)
    {
        var startTime = DateTime.UtcNow;
        _authLogger.LogAuthenticationAttempt(registerDto.Email, "EntraId_With_Fallback", "Registration");

        // Check if user explicitly wants local registration
        if (registerDto.ForceLocalRegistration)
        {
            _authLogger.LogFallbackUsage("EntraId", "Local", "User requested local registration");
            return await ExecuteWithLogging(() => localReg(), "Local", registerDto.Email, startTime);
        }

        // Check if Entra ID is available
        if (!ShouldUseEntraId(registerDto.Email))
        {
            _authLogger.LogFallbackUsage("EntraId", "Local", "Entra ID unavailable for registration");
            return await ExecuteWithLogging(() => localReg(), "Local", registerDto.Email, startTime);
        }

        try
        {
            var entraResult = await ExecuteWithTimeout(() => entraIdReg(), TimeSpan.FromSeconds(20));
            
            if (entraResult.IsSuccess)
            {
                ResetFailureCount("registration");
                return entraResult;
            }

            // Always fallback to local registration if Entra ID fails
            _authLogger.LogFallbackUsage("EntraId", "Local", $"Entra ID registration failed: {entraResult.Message}");
            return await ExecuteWithLogging(() => localReg(), "Local", registerDto.Email, startTime);
        }
        catch (TimeoutException)
        {
            RecordFailure("registration");
            _authLogger.LogFallbackUsage("EntraId", "Local", "Entra ID registration timeout");
            return await ExecuteWithLogging(() => localReg(), "Local", registerDto.Email, startTime);
        }
        catch (Exception ex)
        {
            RecordFailure("registration");
            _logger.LogError(ex, "Unexpected error during Entra ID registration for {Email}", registerDto.Email);
            _authLogger.LogFallbackUsage("EntraId", "Local", $"Registration error: {ex.Message}");
            return await ExecuteWithLogging(() => localReg(), "Local", registerDto.Email, startTime);
        }
    }

    public ServiceResult<AuthUrlResponseDto> GetAuthUrlWithFallback(
        string? email, 
        string? returnUrl, 
        Func<ServiceResult<AuthUrlResponseDto>> entraIdUrl)
    {
        if (!ShouldUseEntraId(email))
        {
            return ServiceResult<AuthUrlResponseDto>.Success(new AuthUrlResponseDto
            {
                UseLocalAuth = true,
                LocalEndpoint = "/api/auth/login",
                Message = "Entra ID unavailable, use local authentication"
            });
        }

        try
        {
            var result = entraIdUrl();
            
            if (result.IsSuccess)
            {
                ResetFailureCount("url_generation");
                return result;
            }

            RecordFailure("url_generation");
            return ServiceResult<AuthUrlResponseDto>.Success(new AuthUrlResponseDto
            {
                UseLocalAuth = true,
                LocalEndpoint = "/api/auth/login",
                Message = "Authentication service temporarily unavailable, use local authentication"
            });
        }
        catch (Exception ex)
        {
            RecordFailure("url_generation");
            _logger.LogError(ex, "Error generating Entra ID URL for {Email}", email);
            
            return ServiceResult<AuthUrlResponseDto>.Success(new AuthUrlResponseDto
            {
                UseLocalAuth = true,
                LocalEndpoint = "/api/auth/login",
                Message = "Authentication service error, use local authentication"
            });
        }
    }

    public bool ShouldUseEntraId(string? email = null)
    {
        // Never use Entra ID in development or if disabled
        if (_environment.IsDevelopment() || Environment.GetEnvironmentVariable("USE_ENTRA_ID")?.ToLower() != "true")
        {
            return false;
        }

        // Check circuit breaker
        if (IsCircuitBreakerOpen())
        {
            return false;
        }

        // Check if Entra ID is healthy
        if (!_errorHandler.IsEntraIdAvailable())
        {
            return false;
        }

        return true;
    }

    public AuthResult CreateFallbackResponse(string operation, string reason, string? redirectEndpoint = null)
    {
        var message = $"External authentication unavailable for {operation}. {reason}";
        
        if (!string.IsNullOrEmpty(redirectEndpoint))
        {
            message += $" Please use {redirectEndpoint}";
        }

        return AuthResult.Success(message, redirectEndpoint);
    }

    private async Task<T> ExecuteWithTimeout<T>(Func<Task<T>> operation, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await operation();
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }

    private async Task<AuthResult> ExecuteWithLogging(
        Func<Task<AuthResult>> operation, 
        string authType, 
        string email, 
        DateTime startTime)
    {
        try
        {
            var result = await operation();
            var duration = DateTime.UtcNow - startTime;

            if (result.IsSuccess)
            {
                _authLogger.LogAuthenticationSuccess(email, authType, result.Data?.User?.Id.ToString() ?? "Unknown", duration);
            }
            else
            {
                _authLogger.LogAuthenticationFailure(email, authType, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _authLogger.LogAuthenticationFailure(email, authType, ex.Message, "EXCEPTION");
            throw;
        }
    }

    private bool ShouldFallbackToLocal(AuthResult entraResult)
    {
        // Don't fall back for user errors (they should retry with Entra ID)
        var noFallbackErrors = new[] { "ACCESS_DENIED", "INVALID_REQUEST", "INVALID_SCOPE" };
        
        if (entraResult.Errors?.Any(e => noFallbackErrors.Contains(e)) == true)
        {
            return false;
        }

        // Fallback for service errors
        return true;
    }

    private void RecordFailure(string operation)
    {
        var key = $"entra_id_{operation}";
        _failureTimestamps[key] = DateTime.UtcNow;
        _failureCounts[key] = _failureCounts.GetValueOrDefault(key, 0) + 1;
        
        _logger.LogWarning(
            "Entra ID failure recorded for {Operation}. Count: {Count}", 
            operation, _failureCounts[key]);
    }

    private void ResetFailureCount(string operation)
    {
        var key = $"entra_id_{operation}";
        if (_failureCounts.ContainsKey(key))
        {
            _failureCounts.Remove(key);
            _failureTimestamps.Remove(key);
            _logger.LogInformation("Entra ID failure count reset for {Operation}", operation);
        }
    }

    private bool IsCircuitBreakerOpen()
    {
        var now = DateTime.UtcNow;
        
        foreach (var kvp in _failureTimestamps.ToList())
        {
            var operation = kvp.Key;
            var timestamp = kvp.Value;
            
            // Reset old failures
            if (now - timestamp > CircuitBreakerTimeout)
            {
                _failureCounts.Remove(operation);
                _failureTimestamps.Remove(operation);
                continue;
            }
            
            // Check if circuit breaker should be open
            if (_failureCounts.GetValueOrDefault(operation, 0) >= MaxFailures)
            {
                _logger.LogWarning(
                    "Circuit breaker open for {Operation} due to {Count} failures", 
                    operation, _failureCounts[operation]);
                return true;
            }
        }
        
        return false;
    }
}