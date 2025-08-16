using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Permission;

public class PermissionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Resource { get; set; }
    public PermissionAction Action { get; set; }
    public PermissionScope? Scope { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RolesCount { get; set; }
}