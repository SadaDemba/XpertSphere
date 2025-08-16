using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace XpertSphere.MonolithApi.Models.Base;

public class Training: AuditableEntity
{
    [Required]
    public Guid UserId { get; set; }
    [MaxLength(100)]
    public string Institution { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? BeginPeriod { get; set; }  = string.Empty;
    
    [MaxLength(20)]
    public string EndPeriod { get; set; }  = string.Empty;
    
    [MaxLength(150)]
    public string FieldOfStudy { get; set; }  = string.Empty;
    
    [MaxLength(60)]
    public string Degree { get; set; }  = string.Empty;
    
    [NotMapped]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}