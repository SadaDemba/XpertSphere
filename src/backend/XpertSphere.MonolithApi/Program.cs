using System.Text.Json.Serialization;
using DotNetEnv;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault Configuration
builder.Services.AddKeyVaultConfiguration(builder);

// Application Insights Telemetry 
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Application Insights logging
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddApplicationInsights();
}

// Infrastructure Services
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddSecurity(builder.Configuration, builder.Environment);

// AutoMapper Configuration
builder.Services.AddAutoMapperConfiguration();

// FluentValidation Configuration
builder.Services.AddFluentValidationConfiguration();

// Health Checks
builder.Services.AddHealthChecks();

// API Services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerDocumentation();

// Application Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// RBAC Services
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();

// Additional Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// Database Migration & Seeding
await app.UseDatabaseAsync();

// Documentation
app.UseSwaggerDocumentation();

// Health Check endpoint
app.MapHealthChecks("/health");

// Security Pipeline (order matters!)
app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

// Application Pipeline
app.MapControllers();

await app.RunAsync();
