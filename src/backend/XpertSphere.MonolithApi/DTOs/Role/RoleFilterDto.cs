using XpertSphere.MonolithApi.DTOs.Base;

namespace XpertSphere.MonolithApi.DTOs.Role;

public class RoleFilterDto: Filter
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsActive { get; set; }
}