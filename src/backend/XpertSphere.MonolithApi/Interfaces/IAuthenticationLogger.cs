namespace XpertSphere.MonolithApi.Interfaces;

public interface IAuthenticationLogger
{
    void LogAuthenticationAttempt(string email, string authType, string source, string? userAgent = null);
    void LogAuthenticationSuccess(string email, string authType, string userId, TimeSpan duration);
    void LogAuthenticationFailure(string email, string authType, string reason, string? errorCode = null);
    void LogEntraIdCallback(string state, string authFlow, bool success, string? error = null);
    void LogAccountLinking(string userId, string email, string externalId, bool success);
    void LogFallbackUsage(string originalAuthType, string fallbackAuthType, string reason);
    void LogSecurityEvent(string eventType, string details, string? userId = null, string? email = null);
}