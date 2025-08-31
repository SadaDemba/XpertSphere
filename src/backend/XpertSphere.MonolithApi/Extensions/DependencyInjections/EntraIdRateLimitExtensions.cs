using XpertSphere.MonolithApi.Services;

namespace XpertSphere.MonolithApi.Extensions.DependencyInjections;


public static class EntraIdRateLimitExtensions
{
    public static IServiceCollection AddEntraIdRateLimit(this IServiceCollection services)
    {
        services.AddScoped<IEntraIdRateLimitService, EntraIdRateLimitService>();
        return services;
    }
    
    public static HttpClient ConfigureForEntraId(this HttpClient httpClient)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "XpertSphere/1.0");
        return httpClient;
    }
}