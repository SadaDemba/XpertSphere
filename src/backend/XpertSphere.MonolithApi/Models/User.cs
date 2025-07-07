using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Models;

public class User : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public Address Address { get; set; } = new();

    [Required]
    public UserType UserType { get; set; }

    // Properties for internal users (nullable for candidates)
    public Guid? OrganizationId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime? HireDate { get; set; }

    // Properties for candidates (nullable for internal users)
    [MaxLength(255)]
    public string? LinkedInProfile { get; set; }

    [MaxLength(500)]
    public string? CvPath { get; set; }

    public string? Skills { get; set; } // JSON array stored as string

    public int? Experience { get; set; } // Years of experience

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    // Common properties
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public bool IsInternal => UserType == UserType.Internal;

    [NotMapped]
    public bool IsCandidate => UserType == UserType.External;
}
