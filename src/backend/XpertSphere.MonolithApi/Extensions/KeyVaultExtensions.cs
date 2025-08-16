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
            "ConnectionStrings:AzureSQL",
            "ApplicationInsights:ConnectionString"
        };
        // Add DEFAULTCONNECTIONSTRING only for Development (Docker DB)
        if (environment == "Development")
        {
            mappings.Add("ConnectionStrings:DefaultConnection");
        }
        else
        {
            // Add Entra ID secrets with environment prefix for Staging/Production
            var envPrefix = environment.Equals("Production", StringComparison.CurrentCultureIgnoreCase) ? "PROD" : "STAGING";
            mappings.Add($"EntraId:B2B:ClientSecret:{envPrefix}");
            mappings.Add($"EntraId:B2C:ClientSecret:{envPrefix}");
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

    private static void LogMissingSecrets(IConfiguration configuration, string environment)
    {
        Console.WriteLine($"INFO - Using environment variables for {environment}");
        
        if (environment != "Development")
        {
            Console.WriteLine("INFO - For production environments, consider using Azure Key Vault");
        }
    }
}