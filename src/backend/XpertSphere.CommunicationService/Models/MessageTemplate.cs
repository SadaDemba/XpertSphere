namespace XpertSphere.CommunicationService.Models;

public class MessageTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public TemplateType Type { get; set; }
    public string Language { get; set; } = "fr-FR";
    public Dictionary<string, string>? Variables { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}

public enum TemplateType
{
    WelcomeCandidate,
    ApplicationReceived,
    ApplicationStatusUpdate,
    InterviewInvitation,
    InterviewReminder,
    TestAssignment,
    RejectionNotice,
    OfferLetter,
    ForgotPassword,
    EmailVerification,
    AccountActivation,
    Custom
}
