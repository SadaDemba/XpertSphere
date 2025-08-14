using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace XpertSphere.MonolithApi.Models.Base;

public class Experience: AuditableEntity
{
    
    [Required]
    public Guid UserId { get; set; }
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string CompanyName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string BeginPeriod { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string EndPeriod { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }
    
    [NotMapped]
    [JsonIgnore]
    public virtual User User { get; set; } =  null!;
}