namespace XpertSphere.MonolithApi.DTOs.RolePermission;

public class RolePermissionDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionResource { get; set; } = string.Empty;
    public string PermissionAction { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}