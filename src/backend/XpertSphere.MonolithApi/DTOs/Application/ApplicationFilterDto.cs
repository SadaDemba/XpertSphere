using XpertSphere.MonolithApi.DTOs.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Application;

public class ApplicationFilterDto : Filter
{
    public Guid? JobOfferId { get; set; }
    public Guid? CandidateId { get; set; }
    public Guid? OrganizationId { get; set; }
    public ApplicationStatus? CurrentStatus { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsCompleted { get; set; }
    public bool? IsInProgress { get; set; }
    public DateTime? AppliedAfter { get; set; }
    public DateTime? AppliedBefore { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }
    public string? CandidateName { get; set; }
    public string? JobTitle { get; set; }
}