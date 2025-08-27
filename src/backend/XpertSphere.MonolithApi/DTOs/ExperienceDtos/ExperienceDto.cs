namespace XpertSphere.MonolithApi.DTOs.ExperienceDtos;

public class ExperienceDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // User info
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
}