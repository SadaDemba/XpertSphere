using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.ExperienceDtos;

public class CreateExperienceDto
{
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Company { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(40)]
    public string Date { get; set; } = string.Empty;
    
    public bool IsCurrent { get; set; }
}