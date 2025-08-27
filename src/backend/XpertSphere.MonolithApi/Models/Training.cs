using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Training: AuditableEntity
{
    [Required]
    public Guid UserId { get; set; }
    [MaxLength(100)]
    public string School { get; set; } = string.Empty;
    
    [MaxLength(40)]
    public string? Period { get; set; }  = string.Empty;
    
    [MaxLength(150)]
    public string Field { get; set; }  = string.Empty;
    
    [MaxLength(60)]
    public string Level { get; set; }  = string.Empty;
    
    [NotMapped]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}