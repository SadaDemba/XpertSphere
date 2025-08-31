namespace XpertSphere.MonolithApi.DTOs.TrainingDtos;

public class TrainingDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Institution { get; set; } = string.Empty;
    public string? BeginPeriod { get; set; } = string.Empty;
    public string EndPeriod { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // User info
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
}