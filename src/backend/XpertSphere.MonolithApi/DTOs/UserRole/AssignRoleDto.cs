using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.UserRole;

public class AssignRoleDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}