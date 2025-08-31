using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.TrainingDtos;

public class UpdateTrainingDto
{
    [MaxLength(100)]
    public string? Institution { get; set; }
    
    [MaxLength(20)]
    public string? BeginPeriod { get; set; }
    
    [MaxLength(20)]
    public string? EndPeriod { get; set; }
    
    [MaxLength(150)]
    public string? FieldOfStudy { get; set; }
    
    [MaxLength(60)]
    public string? Degree { get; set; }
}