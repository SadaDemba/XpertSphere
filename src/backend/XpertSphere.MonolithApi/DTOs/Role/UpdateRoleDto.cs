using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Role;

public class UpdateRoleDto
{
    [Required]
    [MaxLength(150)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}