using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XpertSphere.MonolithApi.Models.Base;

public abstract class AuditableEntity : IAuditableEntity
{

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    // Navigation property for CreatedBy
    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    // Navigation property for UpdatedBy
    [ForeignKey("UpdatedBy")]
    public virtual User? UpdatedByUser { get; set; }
}
