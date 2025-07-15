using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class RolePermission : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }

    // Navigation properties
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;
}
