using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.ExperienceDtos;

public class UpdateExperienceDto
{
    [MaxLength(100)]
    public string? Title { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? Location { get; set; }
    
    [MaxLength(100)]
    public string? Company { get; set; }
    
    [MaxLength(40)]
    public string? Date { get; set; }
    
    
    public bool? IsCurrent { get; set; }
}