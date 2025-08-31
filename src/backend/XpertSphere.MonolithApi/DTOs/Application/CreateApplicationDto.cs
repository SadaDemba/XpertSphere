using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Application;

public class CreateApplicationDto
{
    [MaxLength(2000)]
    public string? CoverLetter { get; set; }

    [MaxLength(1000)]
    public string? AdditionalNotes { get; set; }

    [Required]
    public Guid JobOfferId { get; set; }
}