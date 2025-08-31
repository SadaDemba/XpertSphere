using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.TrainingDtos;

public class CreateTrainingDto
{
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string School { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(40)]
    public string Period { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(150)]
    public string Field { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(60)]
    public string Level { get; set; } = string.Empty;
}