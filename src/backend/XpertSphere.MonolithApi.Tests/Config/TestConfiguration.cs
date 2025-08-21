using Microsoft.Extensions.Configuration;

namespace XpertSphere.MonolithApi.Tests;

public static class TestConfiguration
{
    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Jwt:Secret", "ThisIsATestSecretKeyThatIsLongEnoughForHS256AlgorithmTesting"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpiryInHours", "1"},
                {"Jwt:RefreshTokenExpiryInDays", "7"},
                {"EntraId:TenantId", "test-tenant-id"},
                {"EntraId:ClientId", "test-client-id"},
                {"EntraId:ClientSecret", "test-client-secret"},
                {"EntraId:Instance", "https://login.microsoftonline.com/"},
                {"EntraId:Domain", "test.onmicrosoft.com"},
                {"USE_ENTRA_ID", "false"},
                {"ASPNETCORE_ENVIRONMENT", "Testing"}
            })
            .Build();
    }
}