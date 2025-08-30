using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.User;

public class UpdateUserSkillsDto
{
    [MaxLength(2000)]
    public string? Skills { get; set; }
}