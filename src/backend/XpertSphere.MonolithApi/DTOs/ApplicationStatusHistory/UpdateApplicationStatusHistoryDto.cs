using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;

public class UpdateApplicationStatusHistoryDto
{
    [Required]
    public ApplicationStatus Status { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Comment { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }
}