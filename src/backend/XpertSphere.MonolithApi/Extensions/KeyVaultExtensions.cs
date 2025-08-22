using Azure.Identity;
using Microsoft.Kiota.Abstractions.Extensions;

namespace XpertSphere.MonolithApi.Extensions;

public static class KeyVaultExtensions
{
    private static List<string> GetSecretMappings(string environment)
    {
        var mappings = new List<string>
        {
            "Jwt:Key",
            "Admin:Email",
            "Admin:Password",
            "ConnectionStrings:DefaultConnection",
            "ApplicationInsights:ConnectionString",
            "ConnectionStrings:BlobStorage"
        };
        
        // Add Entra ID secrets with environment prefix for Staging/Production only
        if (environment != "Development")
        {
            var envPrefix = environment.Equals("Production", StringComparison.CurrentCultureIgnoreCase) ? "PROD" : "STAGING";
            mappings.Add($"EntraId:B2B:ClientSecret:{envPrefix}");
            mappings.Add($"EntraId:B2C:ClientSecret:{envPrefix}");
        }
        return mappings;
    }

    public static IServiceCollection AddKeyVaultConfiguration(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        
        // Skip Key Vault in Development - use local .env file
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine("INFO - Development environment detected, using local environment variables (.env file)");
            return services;
        }
        
        // For Staging/Production, Key Vault is required
        var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");
        
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new InvalidOperationException($"KEY_VAULT_URL is required for {environment} environment");
        }

        try
        {
            Console.WriteLine($"INFO - Configuring Key Vault for {environment} environment: {keyVaultUrl}");
            
            // Use specific Managed Identity if AZURE_CLIENT_ID is provided, otherwise use DefaultAzureCredential
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            
            if (string.IsNullOrEmpty(clientId))
            {
                Console.WriteLine("INFO - Using DefaultAzureCredential (no AZURE_CLIENT_ID specified)");
                builder.Configuration.AddAzureKeyVault(
                    new Uri(keyVaultUrl),
                    new DefaultAzureCredential());
            }
            else
            {
                Console.WriteLine($"INFO - Using DefaultAzureCredential with Managed Identity Client ID: {clientId}");
                builder.Configuration.AddAzureKeyVault(
                    new Uri(keyVaultUrl),
                    new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = clientId
                    }));
            }
                
            Console.WriteLine("INFO - Key Vault configuration added successfully");
            ValidateSecretsLoading(builder.Configuration, environment);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to configure Key Vault for {environment}: {ex.Message}", ex);
        }

        return services;
    }
    
    private static void ValidateSecretsLoading(IConfiguration configuration, string environment)
    {
        var missingSecrets = new List<string>();
       
        var secretMappings = GetSecretMappings(environment);
        
        // All secrets are checked (Development uses .env, Staging/Prod uses Key Vault)
        Console.WriteLine("DEBUG - Checking secrets from Key Vault:");
        secretMappings.ToList().ForEach(configPath => {
            var value = configuration[configPath];
            Console.WriteLine($"{configPath}): {(string.IsNullOrEmpty(value) ? "MISSING" : "FOUND")}");
            if (string.IsNullOrEmpty(value))
            {
                missingSecrets.Add(configPath);
            }
        });
        
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
}