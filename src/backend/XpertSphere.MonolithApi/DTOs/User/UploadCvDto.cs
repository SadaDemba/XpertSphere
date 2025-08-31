using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.DTOs.User;

/// <summary>
/// DTO for CV upload operations
/// </summary>
public class UploadCvDto
{
    [Required]
    public required IFormFile CvFile { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool ReplaceExisting { get; set; } = true;

    public bool ExtractInformation { get; set; } = true;
}

/// <summary>
/// Response DTO for CV upload operations
/// </summary>
public class UploadCvResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CvPath { get; set; }
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    
    // Extracted information (if ExtractInformation was true)
    public CvExtractedInfoDto? ExtractedInfo { get; set; }

    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// DTO for information extracted from CV
/// </summary>
public class CvExtractedInfoDto
{
    public string? ExtractedName { get; set; }
    public string? ExtractedEmail { get; set; }
    public string? ExtractedPhone { get; set; }
    public List<string> ExtractedSkills { get; set; } = [];
    public int? ExtractedExperience { get; set; }
    public string? ExtractedSummary { get; set; }
    public List<Training> Education { get; set; } = [];
    public List<Experience> Experience { get; set; } = [];
}
