using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Services;

namespace XpertSphere.MonolithApi.Extensions.DependencyInjections;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Core Application Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IExperienceService, ExperienceService>();
        services.AddScoped<ITrainingService, TrainingService>();
        
        // Job Management Services
        services.AddScoped<IJobOfferService, JobOfferService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IApplicationStatusHistoryService, ApplicationStatusHistoryService>();
        
        // RBAC Services
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        
        // Infrastructure Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // File Management Services
        services.AddScoped<IResumeService, ResumeService>();

        return services;
    }
}