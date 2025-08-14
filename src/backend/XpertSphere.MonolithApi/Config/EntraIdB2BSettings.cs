using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.Config;

public class EntraIdB2BSettings
{
    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public List<string> Scopes { get; set; } = new();

    public List<string> GraphScopes { get; set; } = new()
    {
        "https://graph.microsoft.com/User.Read",
        "https://graph.microsoft.com/User.Read.All",
        "https://graph.microsoft.com/Group.Read.All",
        "https://graph.microsoft.com/Directory.Read.All"
    };

    public List<string> RedirectUris { get; set; } = new();

    public string GraphApiUrl { get; set; } = "https://graph.microsoft.com/v1.0";

    public bool EnableGroupClaims { get; set; } = true;

    public bool EnableRoleClaims { get; set; } = true;

    public List<string> RequiredGroups { get; set; } = new();

    public List<string> RequiredRoles { get; set; } = new();

    public string GroupClaimType { get; set; } = "groups";

    public string RoleClaimType { get; set; } = "roles";
}