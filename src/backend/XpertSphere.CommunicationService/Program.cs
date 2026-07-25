using Microsoft.OpenApi.Models;
using XpertSphere.CommunicationService.Middleware;
using XpertSphere.CommunicationService.Options;
using XpertSphere.CommunicationService.Services;
using XpertSphere.CommunicationService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Clé API partagée. Entrez la valeur configurée dans le champ ci-dessous.",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// Smtp configuration
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Fail-fast au démarrage si la configuration requise est absente, quel que soit l'environnement.
var smtpHost = builder.Configuration["Smtp:Host"];
if (string.IsNullOrWhiteSpace(smtpHost))
{
    throw new InvalidOperationException("Smtp:Host configuration is required but was not provided.");
}

var apiKey = builder.Configuration["ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("ApiKey configuration is required but was not provided.");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health Check endpoint
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
