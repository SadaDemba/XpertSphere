using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Experience: AuditableEntity
{
    
    [Required]
    public Guid UserId { get; set; }
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Company { get; set; } = string.Empty;
    
    [MaxLength(40)]
    public string Date { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }
    
    [NotMapped]
    [JsonIgnore]
    public virtual User User { get; set; } =  null!;
}