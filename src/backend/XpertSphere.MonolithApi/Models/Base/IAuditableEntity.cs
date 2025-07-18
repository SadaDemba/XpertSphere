namespace XpertSphere.MonolithApi.Models.Base
{
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        Guid? CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        Guid? UpdatedBy { get; set; }

        // Navigation properties for audit
        User? CreatedByUser { get; set; }
        User? UpdatedByUser { get; set; }
    }
}
