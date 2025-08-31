using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Services;

public interface IEntraIdRateLimitService
{
    Task<HttpResponseMessage> ExecuteWithRateLimitHandling(Func<Task<HttpResponseMessage>> apiCall, string operation);
    bool IsRateLimited(string operation);
    TimeSpan GetRetryDelay(string operation);
    void RecordApiCall(string operation);
    void RecordRateLimit(string operation, TimeSpan retryAfter);
}

public class EntraIdRateLimitService : IEntraIdRateLimitService
{
    private readonly ILogger<EntraIdRateLimitService> _logger;
    private readonly IAuthenticationLogger _authLogger;
    private readonly EntraIdSettings _entraIdSettings;
    
    // Rate limiting tracking
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
    private static readonly ConcurrentDictionary<string, ApiCallHistory> _apiCallHistory = new();
    
    // Configuration
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);
    private static readonly int MaxRetryAttempts = 3;

    public EntraIdRateLimitService(
        ILogger<EntraIdRateLimitService> logger,
        IAuthenticationLogger authLogger,
        IOptions<EntraIdSettings> entraIdSettings)
    {
        _logger = logger;
        _authLogger = authLogger;
        _entraIdSettings = entraIdSettings.Value;
    }

    public async Task<HttpResponseMessage> ExecuteWithRateLimitHandling(
        Func<Task<HttpResponseMessage>> apiCall, 
        string operation)
    {
        var attempt = 0;
        var startTime = DateTime.UtcNow;

        while (attempt < MaxRetryAttempts)
        {
            attempt++;

            // Check if we're currently rate limited
            if (IsRateLimited(operation))
            {
                var delay = GetRetryDelay(operation);
                _logger.LogWarning(
                    "Operation {Operation} is rate limited. Waiting {Delay}ms before retry {Attempt}",
                    operation, delay.TotalMilliseconds, attempt);

                if (delay > MaxRetryDelay)
                {
                    throw new InvalidOperationException($"Rate limit delay too long: {delay}");
                }

                await Task.Delay(delay);
            }

            try
            {
                RecordApiCall(operation);
                
                using var cts = new CancellationTokenSource(DefaultTimeout);
                var response = await ExecuteWithTimeout(() => apiCall(), DefaultTimeout);
                
                var duration = DateTime.UtcNow - startTime;
                
                // Check response for rate limiting
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = GetRetryAfterFromResponse(response) ?? TimeSpan.FromSeconds(60);
                    RecordRateLimit(operation, retryAfter);
                    
                    _logger.LogWarning(
                        "Rate limited by Microsoft API for {Operation}. Retry after {RetryAfter}s",
                        operation, retryAfter.TotalSeconds);

                    if (attempt < MaxRetryAttempts)
                    {
                        continue; // Retry
                    }
                    
                    throw new HttpRequestException(
                        $"Rate limit exceeded for {operation}. Retry after {retryAfter}");
                }

                // Check for other error responses that might indicate throttling
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    var retryAfter = GetRetryAfterFromResponse(response) ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    
                    _logger.LogWarning(
                        "Service unavailable for {Operation} (Status: {StatusCode}). Retrying in {RetryAfter}s",
                        operation, response.StatusCode, retryAfter.TotalSeconds);

                    if (attempt < MaxRetryAttempts)
                    {
                        await Task.Delay(retryAfter);
                        continue; // Retry
                    }
                }

                // Success or non-retryable error
                LogApiCallResult(operation, response.StatusCode, duration, attempt);
                return response;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex,
                    "Timeout for {Operation} on attempt {Attempt}/{MaxAttempts}",
                    operation, attempt, MaxRetryAttempts);

                if (attempt >= MaxRetryAttempts)
                {
                    throw;
                }

                // Exponential backoff for timeouts
                var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(backoffDelay);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetryAttempts)
            {
                _logger.LogWarning(ex,
                    "HTTP error for {Operation} on attempt {Attempt}/{MaxAttempts}",
                    operation, attempt, MaxRetryAttempts);

                var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(backoffDelay);
            }
        }

        throw new InvalidOperationException($"Max retry attempts exceeded for {operation}");
    }

    public bool IsRateLimited(string operation)
    {
        if (!_rateLimits.TryGetValue(operation, out var rateLimitInfo))
        {
            return false;
        }

        var isLimited = DateTime.UtcNow < rateLimitInfo.RetryAfter;
        
        if (!isLimited)
        {
            // Clean up expired rate limit
            _rateLimits.TryRemove(operation, out _);
        }

        return isLimited;
    }

    public TimeSpan GetRetryDelay(string operation)
    {
        if (!_rateLimits.TryGetValue(operation, out var rateLimitInfo))
        {
            return TimeSpan.Zero;
        }

        var delay = rateLimitInfo.RetryAfter - DateTime.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    public void RecordApiCall(string operation)
    {
        var now = DateTime.UtcNow;
        var history = _apiCallHistory.GetOrAdd(operation, _ => new ApiCallHistory());
        
        lock (history)
        {
            history.Calls.Add(now);
            
            // Keep only calls from the last hour for rate limit tracking
            var oneHourAgo = now.AddHours(-1);
            history.Calls.RemoveAll(call => call < oneHourAgo);
            
            // Log if we're making many calls
            if (history.Calls.Count > 100) // Threshold for monitoring
            {
                _logger.LogWarning(
                    "High API call frequency for {Operation}: {Count} calls in the last hour",
                    operation, history.Calls.Count);
            }
        }
    }

    public void RecordRateLimit(string operation, TimeSpan retryAfter)
    {
        var retryTime = DateTime.UtcNow.Add(retryAfter);
        
        _rateLimits.AddOrUpdate(operation, 
            new RateLimitInfo { RetryAfter = retryTime, RecordedAt = DateTime.UtcNow },
            (key, existing) => new RateLimitInfo { RetryAfter = retryTime, RecordedAt = DateTime.UtcNow });

        _logger.LogWarning(
            "Rate limit recorded for {Operation}. Can retry after {RetryTime}",
            operation, retryTime);
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

    private TimeSpan? GetRetryAfterFromResponse(HttpResponseMessage response)
    {
        // Check Retry-After header
        if (response.Headers.RetryAfter?.Delta.HasValue == true)
        {
            return response.Headers.RetryAfter.Delta.Value;
        }

        if (response.Headers.RetryAfter?.Date.HasValue == true)
        {
            var retryTime = response.Headers.RetryAfter.Date.Value;
            var delay = retryTime - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        }

        // Check custom Microsoft headers
        if (response.Headers.TryGetValues("x-ms-retry-after-ms", out var msValues))
        {
            if (int.TryParse(msValues.FirstOrDefault(), out var retryMs))
            {
                return TimeSpan.FromMilliseconds(retryMs);
            }
        }

        return null;
    }

    private void LogApiCallResult(string operation, HttpStatusCode statusCode, TimeSpan duration, int attempts)
    {
        var logLevel = statusCode switch
        {
            HttpStatusCode.OK => LogLevel.Information,
            HttpStatusCode.Unauthorized => LogLevel.Warning,
            HttpStatusCode.Forbidden => LogLevel.Warning,
            HttpStatusCode.TooManyRequests => LogLevel.Warning,
            HttpStatusCode.InternalServerError => LogLevel.Error,
            HttpStatusCode.ServiceUnavailable => LogLevel.Error,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel,
            "API call completed: {Operation} -> {StatusCode} in {Duration}ms after {Attempts} attempts",
            operation, statusCode, duration.TotalMilliseconds, attempts);

        // Log performance concerns
        if (duration > TimeSpan.FromSeconds(10))
        {
            _logger.LogWarning(
                "Slow API response for {Operation}: {Duration}ms",
                operation, duration.TotalMilliseconds);
        }

        if (attempts > 1)
        {
            _logger.LogInformation(
                "API call required retries: {Operation} succeeded after {Attempts} attempts",
                operation, attempts);
        }
    }

    private class RateLimitInfo
    {
        public DateTime RetryAfter { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    private class ApiCallHistory
    {
        public List<DateTime> Calls { get; set; } = new();
    }
}