using Azure.Storage.Blobs;

namespace XpertSphere.MonolithApi.Extensions;

public static class BlobStorageExtensions
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Try to get from configuration first (includes Key Vault for staging/prod)
        var connectionString = configuration.GetConnectionString("BlobStorage");
        
        // Fallback to environment variable if not found
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BlobStorage");
        }
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("BlobStorage connection string is not configured. Please configure ConnectionStrings:BlobStorage");
        }

        // Register BlobServiceClient as singleton
        services.AddSingleton(provider =>
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            return blobServiceClient;
        });

        return services;
    }
}