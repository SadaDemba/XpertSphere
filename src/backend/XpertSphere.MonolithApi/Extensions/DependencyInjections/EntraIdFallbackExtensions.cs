using XpertSphere.MonolithApi.Services;

namespace XpertSphere.MonolithApi.Extensions.DependencyInjections;

public static class EntraIdFallbackExtensions
{
    public static IServiceCollection AddEntraIdFallback(this IServiceCollection services)
    {
        services.AddScoped<IEntraIdFallbackService, EntraIdFallbackService>();
        services.AddScoped<IEntraIdErrorHandler, EntraIdErrorHandler>();
        return services;
    }
}