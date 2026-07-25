namespace XpertSphere.CommunicationService.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var configuredApiKey = configuration["ApiKey"];
        var providedApiKey = context.Request.Headers[ApiKeyHeaderName].ToString();

        if (string.IsNullOrEmpty(providedApiKey) || !string.Equals(providedApiKey, configuredApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "Missing or invalid API key" });
            return;
        }

        await next(context);
    }
}
