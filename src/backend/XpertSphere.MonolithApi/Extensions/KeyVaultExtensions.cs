using Azure.Identity;

namespace XpertSphere.MonolithApi.Extensions;

public static class KeyVaultExtensions
{
    private static Dictionary<string, string> GetSecretMappings(string environment)
    {
        var mappings = new Dictionary<string, string>
        {
            // JWT Secrets
            ["JWT-KEY"] = "Jwt:Key",
            
            // Database Connection Strings
            ["AZURESQLCONNECTIONSTRING"] = "ConnectionStrings:AzureSqlConnectionString",
            
            // Admin Configuration
            ["ADMIN-EMAIL"] = "Seeding:PlatformSuperAdmin:Email",
            ["ADMIN-PASSWORD"] = "Seeding:PlatformSuperAdmin:Password",
            
            // Application Insights
            ["APPLICATIONINSIGHTS-CONNECTION-STRING"] = "ApplicationInsights:ConnectionString"
        };

        // Add DEFAULTCONNECTIONSTRING only for Development (Docker DB)
        if (environment == "Development")
        {
            mappings["DEFAULTCONNECTIONSTRING"] = "ConnectionStrings:DefaultConnection";
        }

        // Add Entra ID secrets with environment prefix for Staging/Production
        if (environment != "Development")
        {
            var envPrefix = environment.ToUpper() == "Production" ? "PROD" : "STAGING";
            mappings[$"{envPrefix}-ENTRAID-B2B-CLIENTSECRET"] = "EntraId:B2B:ClientSecret";
            mappings[$"{envPrefix}-ENTRAID-B2C-CLIENTSECRET"] = "EntraId:B2C:ClientSecret";
        }

        return mappings;
    }

    public static IServiceCollection AddKeyVaultConfiguration(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var environment = builder.Environment.EnvironmentName;
        
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            Console.WriteLine("INFO - Key Vault URL not configured, using environment variables only");
            LogMissingSecrets(builder.Configuration, environment);
            return services;
        }

        try
        {
            Console.WriteLine($"INFO - Configuring Key Vault for {environment} environment: {keyVaultUrl}");
            
            var credential = CreateAzureCredential(clientId);
            
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                credential);
                
            Console.WriteLine("INFO - Key Vault configuration added successfully");
            ValidateSecretsLoading(builder.Configuration, environment);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR - Key Vault configuration failed: {ex.Message}");
            Console.WriteLine("INFO - Falling back to environment variables");
            LogMissingSecrets(builder.Configuration, environment);
        }

        return services;
    }
    
    private static DefaultAzureCredential CreateAzureCredential(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            Console.WriteLine("INFO - Using default Azure credential");
            return new DefaultAzureCredential();
        }
        
        Console.WriteLine($"INFO - Using managed identity with Client ID: {clientId}");
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = clientId
        });
    }

    private static void ValidateSecretsLoading(IConfiguration configuration, string environment)
    {
        var missingSecrets = new List<string>();
        var secretMappings = GetSecretMappings(environment);
        
        // All secrets are checked (Development uses .env, Staging/Prod uses Key Vault)
        var secretsToCheck = secretMappings;

        foreach (var (secretName, configPath) in secretsToCheck)
        {
            var value = configuration[configPath];
            if (string.IsNullOrEmpty(value) || value.Contains("***MOVED_TO_ENV***"))
            {
                missingSecrets.Add($"{secretName} -> {configPath}");
            }
        }

        if (missingSecrets.Any())
        {
            Console.WriteLine($"WARNING - Missing or placeholder secrets in {environment}:");
            foreach (var secret in missingSecrets)
            {
                Console.WriteLine($"  - {secret}");
            }
        }
        else
        {
            Console.WriteLine($"INFO - All required secrets loaded successfully for {environment}");
        }
    }

    private static void LogMissingSecrets(IConfiguration configuration, string environment)
    {
        Console.WriteLine($"INFO - Using environment variables for {environment}");
        
        if (environment != "Development")
        {
            Console.WriteLine("INFO - For production environments, consider using Azure Key Vault");
        }
    }
}