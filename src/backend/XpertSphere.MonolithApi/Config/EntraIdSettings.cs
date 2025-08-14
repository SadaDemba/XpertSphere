using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.Config;

public class EntraIdSettings
{
    [Required]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    public EntraIdB2BSettings B2B { get; set; } = new();

    [Required]
    public EntraIdB2CSettings B2C { get; set; } = new();

    public List<string> ValidIssuers { get; set; } = new();
    public List<string> ValidAudiences { get; set; } = new();
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}