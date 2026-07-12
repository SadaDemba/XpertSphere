namespace XpertSphere.CommunicationService.Models;

public class NotificationMessage
{
    public string NotificationId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; } = false;
    public string? ActionUrl { get; set; }
    public string? IconUrl { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? GroupKey { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    ApplicationStatusChange,
    NewApplication,
    InterviewScheduled,
    TestAssigned,
    Feedback,
    SystemUpdate
}

public enum NotificationChannel
{
    InApp,
    Email,
    Push,
    SMS,
    All
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
