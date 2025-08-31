using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class UserRole : AuditableEntity
{

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedBy { get; set; } // UserId qui a assigné ce rôle

    public bool IsActive { get; set; } = true;

    public DateTime? ExpiresAt { get; set; } // Optionnel : expiration du rôle

    // Navigation properties
    [ForeignKey("UserId")]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleId")]
    [JsonIgnore]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("AssignedBy")]
    [JsonIgnore]
    public virtual User? AssignedByUser { get; set; }
}
