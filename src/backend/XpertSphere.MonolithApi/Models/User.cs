using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;
using Microsoft.AspNetCore.Identity;

namespace XpertSphere.MonolithApi.Models;

public class User : IdentityUser<Guid>, IAuditableEntity
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Address Address { get; set; } = new();

    [Required]
    public UserType UserType { get; set; }

    public Guid? OrganizationId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime? HireDate { get; set; }

    [MaxLength(255)]
    public string? LinkedInProfile { get; set; }

    [MaxLength(500)]
    public string? CvPath { get; set; }

    public string? Skills { get; set; }

    public int? Experience { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    // Common properties
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("UpdatedBy")]
    public virtual User? UpdatedByUser { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } =[];

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
