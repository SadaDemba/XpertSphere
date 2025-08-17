using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.Application;

public class UpdateApplicationDto
{
    [MaxLength(2000)]
    public string? CoverLetter { get; set; }

    [MaxLength(1000)]
    public string? AdditionalNotes { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }
}