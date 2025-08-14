using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.User;

public record UserDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Organization info (for internal users)
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public DateTime? HireDate { get; set; }

    // Candidate info (for external users)
    public string? LinkedInProfile { get; set; }
    public string? Skills { get; set; }
    public int? YearsOfExperience { get; set; }
    public List<Training>? Trainings { get; set; } = [];
    public List<Experience>? Experiences { get; set; } = [];
    public decimal? DesiredSalary { get; set; }
    public DateTime? Availability { get; set; }
    public string FullName { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public int ProfileCompletionPercentage { get; init; }

    // Address
    public AddressDto? Address { get; set; }


    // Roles
    public List<string>? Roles { get; set; }

}

public class UserRoleDto
{
    public Guid RoleId { get; set; }
    public required string RoleName { get; set; }
    public required string RoleDisplayName { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
