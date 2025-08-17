using System.Text.Json.Serialization;
using DotNetEnv;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Extensions.DependencyInjections;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Middleware;
using XpertSphere.MonolithApi.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSecurity(builder.Configuration, builder.Environment);
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

// Authentication Error Handling & Logging Services
builder.Services.AddAuthenticationLogging();
builder.Services.AddEntraIdFallback();
builder.Services.AddEntraIdRateLimit();

// HTTP Client for Entra ID APIs
builder.Services.AddHttpClient("EntraId", client => client.ConfigureForEntraId());

// Additional Services
builder.Services.AddHttpContextAccessor();

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

// Claims enrichment only needed for EntraID in production
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<ClaimsEnrichmentMiddleware>();
}

app.UseAuthorization();

// Application Pipeline
app.MapControllers();

await app.RunAsync();
