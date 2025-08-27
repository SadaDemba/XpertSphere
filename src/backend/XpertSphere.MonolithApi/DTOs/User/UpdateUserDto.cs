using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.User;

public class UpdateUserDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public Guid? OrganizationId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime? HireDate { get; set; }

    [MaxLength(255)]
    public string? LinkedInProfile { get; set; }

    public string? Skills { get; set; }

    public int? YearsOfExperience { get; set; }

    public List<Training>? Trainings { get; set; } = [];
    
    public List<Experience>? Experiences { get; set; } = [];

    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    public bool? IsActive { get; set; }

    public AddressDto? Address { get; set; }
}
