namespace XpertSphere.CommunicationService.Models;

public class EmailMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public Dictionary<string, string>? TemplateData { get; set; }
    public string? TemplateName { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledFor { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? CorrelationId { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum EmailStatus
{
    Pending,
    Queued,
    Sending,
    Sent,
    Failed,
    Scheduled
}
