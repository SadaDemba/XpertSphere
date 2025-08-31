using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;

public class ApplicationStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string RatingDescription { get; set; } = string.Empty;
    public bool HasRating { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public string UpdatedByUserName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}