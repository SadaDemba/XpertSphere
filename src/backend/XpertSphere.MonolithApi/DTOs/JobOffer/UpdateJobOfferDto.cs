using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.JobOffer;

public class UpdateJobOfferDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Requirements { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public WorkMode? WorkMode { get; set; }

    public ContractType? ContractType { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    [MaxLength(10)]
    public string? SalaryCurrency { get; set; }

    public DateTime? ExpiresAt { get; set; }
}