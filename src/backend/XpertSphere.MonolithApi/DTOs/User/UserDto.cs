using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public UserType UserType { get; set; }
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
    public int? Experience { get; set; }
    public decimal? DesiredSalary { get; set; }
    public DateTime? Availability { get; set; }

    // Address
    public AddressDto? Address { get; set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsInternal => UserType == UserType.Internal;
    public bool IsCandidate => UserType == UserType.External;

    // Roles
    public List<UserRoleDto>? UserRoles { get; set; }
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
