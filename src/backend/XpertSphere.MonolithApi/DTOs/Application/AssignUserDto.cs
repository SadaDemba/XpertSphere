using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Application;

public class AssignUserDto
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public AssignmentType AssignmentType { get; set; }
}