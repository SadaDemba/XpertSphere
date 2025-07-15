using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Role : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(150)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
