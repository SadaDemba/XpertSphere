using XpertSphere.MonolithApi.DTOs.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Permission;

public class PermissionFilterDto : Filter
{
    public string? Resource { get; set; }
    public PermissionAction? Action { get; set; }
    public string? Category { get; set; }
    public PermissionScope? Scope { get; set; }
}