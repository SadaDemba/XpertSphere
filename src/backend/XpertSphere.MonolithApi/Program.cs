using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load .env file if it exists
var envFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env");
if (File.Exists(envFile))
{
    foreach (var line in await File.ReadAllLinesAsync(envFile))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

// Infrastructure Services
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddSecurity(builder.Configuration, builder.Environment);

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
app.UseAuthorization();

// Application Pipeline
app.MapControllers();

await app.RunAsync();
