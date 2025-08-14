using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.Config;

public class EntraIdB2CSettings
{
    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string Domain { get; set; } = string.Empty;

    [Required]
    public string SignUpSignInPolicyId { get; set; } = string.Empty;

    public string ResetPasswordPolicyId { get; set; } = string.Empty;

    public string EditProfilePolicyId { get; set; } = string.Empty;

    [Required]
    public List<string> Scopes { get; set; } = new();

    public List<string> RedirectUris { get; set; } = new();

    public List<string> PostLogoutRedirectUris { get; set; } = new();

    public bool EnableCustomAttributes { get; set; } = true;

    public List<string> CustomAttributes { get; set; } = new();

    public PasswordPolicySettings PasswordPolicy { get; set; } = new();

    public string MetadataUrl => $"{Authority}/v2.0/.well-known/openid_configuration?p={SignUpSignInPolicyId}";
}

public class PasswordPolicySettings
{
    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 64;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigits { get; set; } = true;
    public bool RequireSpecialCharacters { get; set; } = true;
    public int MinimumAge { get; set; } = 1;
    public int MaximumAge { get; set; } = 90;
    public int PasswordHistory { get; set; } = 5;
}