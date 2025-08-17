using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Models;

public class ApplicationStatusHistory
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public ApplicationStatus Status { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Comment { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    [Required]
    public Guid UpdatedByUserId { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ApplicationId")]
    [JsonIgnore]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey("UpdatedByUserId")]
    [JsonIgnore]
    public virtual User UpdatedByUser { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public string StatusDisplayName => Status switch
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
        _ => Status.ToString()
    };

    [NotMapped]
    public bool HasRating => Rating.HasValue;

    [NotMapped]
    public string RatingDescription => Rating switch
    {
        1 => "Poor",
        2 => "Below Average",
        3 => "Average",
        4 => "Good",
        5 => "Excellent",
        _ => "Not Rated"
    };
}