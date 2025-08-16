using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Organization : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }
    
    public Address Address { get; set; } = new();

    [EmailAddress]
    [MaxLength(255)]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? Industry { get; set; }

    public OrganizationSize? Size { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual ICollection<User> Users { get; set; } = [];
}
