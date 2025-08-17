using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class Application : AuditableEntity
{
    [MaxLength(2000)]
    public string? CoverLetter { get; set; }

    [MaxLength(1000)]
    public string? AdditionalNotes { get; set; }

    [Required]
    public ApplicationStatus CurrentStatus { get; set; } = ApplicationStatus.Applied;

    [Range(1, 5)]
    public int? Rating { get; set; }

    [Required]
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUpdatedAt { get; set; }

    [Required]
    public Guid JobOfferId { get; set; }

    [Required]
    public Guid CandidateId { get; set; }

    public Guid? AssignedTechnicalEvaluatorId { get; set; }

    public Guid? AssignedManagerId { get; set; }

    // Navigation properties
    [ForeignKey("JobOfferId")]
    [JsonIgnore]
    public virtual JobOffer JobOffer { get; set; } = null!;

    [ForeignKey("CandidateId")]
    [JsonIgnore]
    public virtual User Candidate { get; set; } = null!;

    [ForeignKey("AssignedTechnicalEvaluatorId")]
    [JsonIgnore]
    public virtual User? AssignedTechnicalEvaluator { get; set; }

    [ForeignKey("AssignedManagerId")]
    [JsonIgnore]
    public virtual User? AssignedManager { get; set; }

    [JsonIgnore]
    public virtual ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];

    // Computed properties
    [NotMapped]
    public bool IsActive => CurrentStatus != ApplicationStatus.Rejected && 
                           CurrentStatus != ApplicationStatus.Withdrawn && 
                           CurrentStatus != ApplicationStatus.Accepted;

    [NotMapped]
    public bool IsCompleted => CurrentStatus == ApplicationStatus.Accepted || 
                              CurrentStatus == ApplicationStatus.Rejected || 
                              CurrentStatus == ApplicationStatus.Withdrawn;

    [NotMapped]
    public bool IsInProgress => CurrentStatus != ApplicationStatus.Applied && IsActive;

    [NotMapped]
    public string StatusDisplayName => CurrentStatus switch
    {
        ApplicationStatus.Applied => "Application Submitted",
        ApplicationStatus.Reviewed => "Under Review",
        ApplicationStatus.PhoneScreening => "Phone Screening",
        ApplicationStatus.TechnicalTest => "Technical Assessment",
        ApplicationStatus.TechnicalInterview => "Technical Interview",
        ApplicationStatus.FinalInterview => "Final Interview",
        ApplicationStatus.OfferMade => "Offer Extended",
        ApplicationStatus.Accepted => "Offer Accepted",
        ApplicationStatus.Rejected => "Application Rejected",
        ApplicationStatus.Withdrawn => "Application Withdrawn",
        _ => CurrentStatus.ToString()
    };

}