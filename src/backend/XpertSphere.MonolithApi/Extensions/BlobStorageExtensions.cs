using Azure.Storage.Blobs;

namespace XpertSphere.MonolithApi.Extensions;

public static class BlobStorageExtensions
{
    private const string AzuriteConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Try to get from configuration first (includes Key Vault for staging/prod)
        var connectionString = configuration.GetConnectionString("BlobStorage");

        // Fallback to environment variable if not found
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BlobStorage");
        }

        // Fallback to the canonical Azurite connection string in Development only
        if (string.IsNullOrEmpty(connectionString) && environment.IsDevelopment())
        {
            connectionString = AzuriteConnectionString;
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "BlobStorage connection string is not configured. Please configure ConnectionStrings:BlobStorage");
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
