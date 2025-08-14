using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.RolePermission;

public class AssignPermissionDto
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }
}