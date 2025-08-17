using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Application;

public class AssignManagerDto
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public Guid ManagerId { get; set; }
}