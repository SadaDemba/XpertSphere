using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.User;

public class CreateUserDto
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

    // For internal users
    public Guid? OrganizationId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime? HireDate { get; set; }

    // For candidates
    [MaxLength(255)]
    public string? LinkedInProfile { get; set; }

    public string? Skills { get; set; }

    public int? YearsOfExperience { get; set; }
    
    public List<Training>? Trainings { get; set; } = [];
    
    public List<Experience>? Experiences { get; set; } = [];

    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    // Address
    public AddressDto? Address { get; set; }
}

public class AddressDto
{
    [MaxLength(10)]
    public string? StreetNumber { get; set; }

    [MaxLength(255)]
    public string? StreetName { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "France";

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }
}
