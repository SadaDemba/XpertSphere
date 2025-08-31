using FluentValidation;
using XpertSphere.MonolithApi.Validators.User;

namespace XpertSphere.MonolithApi.Extensions;

/// <summary>
/// Extension methods for configuring FluentValidation
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Add FluentValidation configuration to services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddFluentValidationConfiguration(this IServiceCollection services)
    {
        // Add validators from the current assembly (new approach)
        services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>(ServiceLifetime.Scoped);
        
        // Configure global validation behavior
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;

        return services;
    }
}
