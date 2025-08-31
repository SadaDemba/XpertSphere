using System.Text.Json.Serialization;
using DotNetEnv;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Extensions.DependencyInjections;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Middleware;
using XpertSphere.MonolithApi.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Authentication configuration from environment
var useEntraId = Environment.GetEnvironmentVariable("USE_ENTRA_ID")?.ToLower() == "true";

// CORS Configuration - Only for Development (local testing)
if (builder.Environment.IsDevelopment())
{
    var corsOrigins = Environment.GetEnvironmentVariable("CORS__ALLOWED_ORIGINS")?.Split(',') ?? [];
    
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });
}

if (!builder.Environment.IsDevelopment())
{
    // Azure Key Vault Configuration
    builder.Services.AddKeyVaultConfiguration(builder);
    
    // Application Insights Telemetry 
    builder.Services.AddApplicationInsightsTelemetry();
    
    // Logging configuration
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    
    builder.Logging.AddApplicationInsights();
}

// Infrastructure Services
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddSecurity(builder.Configuration, builder.Environment, useEntraId);
builder.Services.AddBlobStorage(builder.Configuration);

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
builder.Services.AddApplicationServices();


builder.Services.AddAuthenticationLogging();
if (useEntraId)
{
    // Authentication Error Handling & Logging Services
    builder.Services.AddEntraIdFallback();
    builder.Services.AddEntraIdRateLimit();
    
    // HTTP Client for Entra ID APIs
    builder.Services.AddHttpClient("EntraId", client => client.ConfigureForEntraId());
}

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Database Migration & Seeding
await app.UseDatabaseAsync();

// Documentation
app.UseSwaggerDocumentation();

// Health Check endpoint
app.MapHealthChecks("/health");

// Security Pipeline
app.UseHttpsRedirection();
app.UseCookiePolicy();

// Use CORS only in Development
if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

app.UseAuthentication();

if (!app.Environment.IsDevelopment() && useEntraId)
{
    // Claims enrichment only needed for EntraID in production
    app.UseMiddleware<ClaimsEnrichmentMiddleware>();
}

app.UseAuthorization();

// Application Pipeline
app.MapControllers();

await app.RunAsync();

// Make the Program accessible for the testing project
public partial class Program { }
