using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Models;

public class Organization : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }

    // Organization address
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
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
