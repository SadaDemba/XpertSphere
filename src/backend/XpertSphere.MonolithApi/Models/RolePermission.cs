using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class RolePermission : AuditableEntity
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }

    // Navigation properties
    [ForeignKey("RoleId")]
    [JsonIgnore]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    [JsonIgnore]
    public virtual Permission Permission { get; set; } = null!;
}
