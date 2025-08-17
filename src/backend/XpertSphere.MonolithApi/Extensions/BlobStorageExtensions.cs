using Azure.Storage.Blobs;

namespace XpertSphere.MonolithApi.Extensions;

public static class BlobStorageExtensions
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Try to get from environment variable first, then fallback to configuration
        var connectionString = Environment.GetEnvironmentVariable("BLOBSTORAGECONNECTIONSTRING") 
                                ?? configuration.GetConnectionString("BlobStorage");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("BlobStorage connection string is not configured. Please set BLOBSTORAGECONNECTIONSTRING environment variable.");
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