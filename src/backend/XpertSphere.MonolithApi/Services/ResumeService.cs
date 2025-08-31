using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class ResumeService : IResumeService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<ResumeService> _logger;
    private readonly string _containerName = "resumes";
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx" };
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public ResumeService(BlobServiceClient blobServiceClient, ILogger<ResumeService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<ServiceResult<string>> UploadResumeAsync(IFormFile file, Guid userId)
    {
        try
        {
            // Validation
            var validationResult = ValidateFile(file);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // Get or create container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Generate unique blob name
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"{userId}/resume_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}";

            // Upload file
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(fileExtension)
                },
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "originalFileName", file.FileName },
                    { "uploadedAt", DateTime.UtcNow.ToString("O") }
                }
            };

            await blobClient.UploadAsync(stream, uploadOptions);

            _logger.LogInformation("Resume uploaded successfully for user {UserId}: {BlobName}", userId, blobName);

            return ServiceResult<string>.Success(blobClient.Uri.ToString(), "Resume uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading resume for user {UserId}", userId);
            return ServiceResult<string>.InternalError("An error occurred while uploading the resume");
        }
    }

    public async Task<ServiceResult<string>> UpdateResumeAsync(IFormFile file, Guid userId, string? existingResumePath = null)
    {
        try
        {
            // Delete existing resume if provided
            if (!string.IsNullOrEmpty(existingResumePath))
            {
                await DeleteResumeAsync(existingResumePath);
            }

            // Upload new resume
            return await UploadResumeAsync(file, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating resume for user {UserId}", userId);
            return ServiceResult<string>.InternalError("An error occurred while updating the resume");
        }
    }

    public async Task<ServiceResult> DeleteResumeAsync(string resumePath)
    {
        try
        {
            if (string.IsNullOrEmpty(resumePath))
            {
                return ServiceResult.Failure("Resume path is required");
            }

            // Extract blob name from URL
            var uri = new Uri(resumePath);
            var blobName = uri.Segments.Skip(2).Aggregate((a, b) => a + b); // Skip /container/

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Resume deleted successfully: {ResumePath}", resumePath);
                return ServiceResult.Success("Resume deleted successfully");
            }
            else
            {
                _logger.LogWarning("Resume not found for deletion: {ResumePath}", resumePath);
                return ServiceResult.NotFound("Resume not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resume: {ResumePath}", resumePath);
            return ServiceResult.InternalError("An error occurred while deleting the resume");
        }
    }

    public async Task<ServiceResult<Stream>> DownloadResumeAsync(string resumePath)
    {
        try
        {
            if (string.IsNullOrEmpty(resumePath))
            {
                return ServiceResult<Stream>.Failure("Resume path is required");
            }

            // Extract blob name from URL
            var uri = new Uri(resumePath);
            var blobName = uri.Segments.Skip(2).Aggregate((a, b) => a + b);

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                return ServiceResult<Stream>.NotFound("Resume not found");
            }

            var response = await blobClient.OpenReadAsync();
            return ServiceResult<Stream>.Success(response, "Resume downloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading resume: {ResumePath}", resumePath);
            return ServiceResult<Stream>.InternalError("An error occurred while downloading the resume");
        }
    }

    public async Task<ServiceResult<ResumeMetadata>> GetResumeMetadataAsync(string resumePath)
    {
        try
        {
            if (string.IsNullOrEmpty(resumePath))
            {
                return ServiceResult<ResumeMetadata>.Failure("Resume path is required");
            }

            var uri = new Uri(resumePath);
            var blobName = uri.Segments.Skip(2).Aggregate((a, b) => a + b);

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync();
            
            var metadata = new ResumeMetadata
            {
                FileName = properties.Value.Metadata.TryGetValue("originalFileName", out var fileName) ? fileName : "resume.pdf",
                Size = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                LastModified = properties.Value.LastModified.DateTime,
                UploadedAt = properties.Value.Metadata.TryGetValue("uploadedAt", out var uploadedAtStr) && DateTime.TryParse(uploadedAtStr, out var uploadedAt) 
                    ? uploadedAt : properties.Value.LastModified.DateTime
            };

            return ServiceResult<ResumeMetadata>.Success(metadata, "Metadata retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resume metadata: {ResumePath}", resumePath);
            return ServiceResult<ResumeMetadata>.InternalError("An error occurred while retrieving resume metadata");
        }
    }

    private ServiceResult<string> ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return ServiceResult<string>.ValidationError(["No file provided"]);
        }

        if (file.Length > _maxFileSize)
        {
            return ServiceResult<string>.ValidationError([$"File size exceeds {_maxFileSize / (1024 * 1024)}MB limit"]);
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(fileExtension))
        {
            return ServiceResult<string>.ValidationError([$"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}"]);
        }

        return ServiceResult<string>.Success("", "File validation passed");
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}