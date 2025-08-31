namespace XpertSphere.MonolithApi.DTOs.Role;

public class RoleDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UsersCount { get; set; }
    public int PermissionsCount { get; set; }
}