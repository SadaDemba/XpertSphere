using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.JobOffer;

public class CreateJobOfferDto
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

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    [MaxLength(10)]
    public string? SalaryCurrency { get; set; } = "EUR";

    public DateTime? ExpiresAt { get; set; }
}