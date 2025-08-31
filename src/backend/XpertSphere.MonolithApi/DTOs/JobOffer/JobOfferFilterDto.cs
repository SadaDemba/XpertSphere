using XpertSphere.MonolithApi.DTOs.Base;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.JobOffer;

public class JobOfferFilterDto : Filter
{
    public string? Title { get; set; }
    public string? Location { get; set; }
    public WorkMode? WorkMode { get; set; }
    public ContractType? ContractType { get; set; }
    public JobOfferStatus? Status { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsExpired { get; set; }
    public DateTime? PublishedAfter { get; set; }
    public DateTime? PublishedBefore { get; set; }
    public DateTime? ExpiresAfter { get; set; }
    public DateTime? ExpiresBefore { get; set; }
}