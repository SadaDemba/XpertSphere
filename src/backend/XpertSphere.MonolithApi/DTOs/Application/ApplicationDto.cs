using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Application;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public string? CoverLetter { get; set; }
    public string? AdditionalNotes { get; set; }
    public ApplicationStatus CurrentStatus { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public Guid JobOfferId { get; set; }
    public string JobOfferTitle { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    
    public Guid? AssignedTechnicalEvaluatorId { get; set; }
    public string? AssignedTechnicalEvaluatorName { get; set; }
    
    public Guid? AssignedManagerId { get; set; }
    public string? AssignedManagerName { get; set; }
    
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsInProgress { get; set; }
    
    public List<ApplicationStatusHistoryDto> StatusHistory { get; set; } = [];
}