namespace XpertSphere.MonolithApi.DTOs.UserRole;

public class UserRoleDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }
    public string? AssignedByName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}