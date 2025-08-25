using AutoMapper;
using XpertSphere.MonolithApi.Mappings;

namespace XpertSphere.MonolithApi.Extensions;

/// <summary>
/// Extension methods for configuring AutoMapper
/// </summary>
public static class AutoMapperExtensions
{
    /// <summary>
    /// Add AutoMapper configuration to services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<OrganizationMappingProfile>();
            cfg.AddProfile<RoleMappingProfile>();
            cfg.AddProfile<PermissionMappingProfile>();
            cfg.AddProfile<RolePermissionMappingProfile>();
            cfg.AddProfile<UserRoleMappingProfile>();
            cfg.AddProfile<JobOfferMappingProfile>();
            cfg.AddProfile<AuthMappingProfile>();
        });


        services.Configure<MapperConfigurationExpression>(cfg =>
        {
            cfg.AllowNullCollections = true;
            cfg.AllowNullDestinationValues = true;
        });

        return services;
    }
}
