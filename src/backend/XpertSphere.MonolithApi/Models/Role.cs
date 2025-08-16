using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Role : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(150)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    //To remove later... I'm just gonna use the isActive Attribute which is in UserRole
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [JsonIgnore]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    [JsonIgnore]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
