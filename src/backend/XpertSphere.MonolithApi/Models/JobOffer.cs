using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Models;

public class JobOffer : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Description { get; set; }

    [Required]
    public required string Requirements { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [Required]
    public WorkMode WorkMode { get; set; }

    [Required]
    public ContractType ContractType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalaryMin { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalaryMax { get; set; }

    [MaxLength(10)]
    public string? SalaryCurrency { get; set; } = "EUR";

    [Required]
    public JobOfferStatus Status { get; set; } = JobOfferStatus.Draft;

    public DateTime? PublishedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("OrganizationId")]
    [JsonIgnore]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("CreatedByUserId")]
    [JsonIgnore]
    public virtual User CreatedByUserNavigation { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public bool IsActive => Status == JobOfferStatus.Published && 
                           (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;

    [NotMapped]
    public bool RequiresLocation => WorkMode != WorkMode.FullRemote;

    // Methods
    public void Publish()
    {
        if (Status != JobOfferStatus.Draft)
            throw new InvalidOperationException("Only draft job offers can be published");

        if (RequiresLocation && string.IsNullOrWhiteSpace(Location))
            throw new InvalidOperationException("Location is required for non-remote positions");

        Status = JobOfferStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status != JobOfferStatus.Published)
            throw new InvalidOperationException("Only published job offers can be closed");

        Status = JobOfferStatus.Closed;
    }

    public void Validate()
    {
        if (RequiresLocation && string.IsNullOrWhiteSpace(Location))
            throw new ValidationException("Location is required when WorkMode is not FullRemote");

        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMin > SalaryMax)
            throw new ValidationException("SalaryMin cannot be greater than SalaryMax");

        if (ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("ExpiresAt must be in the future");
    }
}