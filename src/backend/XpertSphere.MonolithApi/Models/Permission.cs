using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Models;

public class Permission : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Resource { get; set; }

    [Required]
    public PermissionAction Action { get; set; }

    public PermissionScope? Scope { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
