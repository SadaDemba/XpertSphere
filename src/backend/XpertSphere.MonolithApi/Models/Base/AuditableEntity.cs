using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace XpertSphere.MonolithApi.Models.Base;


public abstract class AuditableEntity : IAuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    // Navigation property for CreatedBy
    [ForeignKey("CreatedBy")]
    [JsonIgnore]
    public virtual User? CreatedByUser { get; set; }

    // Navigation property for UpdatedBy
    [ForeignKey("UpdatedBy")]
    [JsonIgnore]
    public virtual User? UpdatedByUser { get; set; }
}
