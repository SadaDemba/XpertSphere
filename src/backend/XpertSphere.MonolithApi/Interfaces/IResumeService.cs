using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IResumeService
{
    Task<ServiceResult<string>> UploadResumeAsync(IFormFile file, Guid userId);
    Task<ServiceResult<string>> UpdateResumeAsync(IFormFile file, Guid userId, string? existingResumePath = null);
    Task<ServiceResult> DeleteResumeAsync(string resumePath);
    Task<ServiceResult<Stream>> DownloadResumeAsync(string resumePath);
    Task<ServiceResult<ResumeMetadata>> GetResumeMetadataAsync(string resumePath);
}

public class ResumeMetadata
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public DateTime UploadedAt { get; set; }
}