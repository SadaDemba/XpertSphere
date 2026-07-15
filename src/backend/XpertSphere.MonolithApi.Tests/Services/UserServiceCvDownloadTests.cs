using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Tests.Services;

/// <summary>
/// Tests for <see cref="UserService.GetCvForDownloadAsync"/> and the upload/download round-trip,
/// covering the acceptance criteria of secure-cv-download.md that are testable at the service layer
/// (criteria 1, 5, 6 and 8 - the storage desynchronisation branch of the "Comportement cible" section).
/// </summary>
public class UserServiceCvDownloadTests : IDisposable
{
    private readonly Mock<IValidator<CreateUserDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateUserDto>> _mockUpdateValidator;
    private readonly Mock<IValidator<UserFilterDto>> _mockFilterValidator;
    private readonly Mock<IValidator<UploadCvDto>> _mockUploadCvValidator;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly XpertSphereDbContext _context;

    public UserServiceCvDownloadTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockCreateValidator = new Mock<IValidator<CreateUserDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateUserDto>>();
        _mockFilterValidator = new Mock<IValidator<UserFilterDto>>();
        _mockUploadCvValidator = new Mock<IValidator<UploadCvDto>>();
        _mockLogger = MockHelper.CreateMockLogger<UserService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task GetCvForDownloadAsync_WithExistingCv_ShouldReturnContentFileNameAndContentType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cvPath = "https://fakeaccount.blob.core.windows.net/resumes/testfile.pdf";
        var user = new User
        {
            Id = userId,
            Email = "candidate@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            IsActive = true,
            CvPath = cvPath
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fileBytes = new byte[] { 1, 2, 3, 4, 5 };
        var fakeResumeService = new FakeResumeService();
        fakeResumeService.Seed(cvPath, fileBytes, "cv_jane_doe.pdf", "application/pdf");

        var userService = CreateUserService(fakeResumeService);

        // Act
        var result = await userService.GetCvForDownloadAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FileName.Should().Be("cv_jane_doe.pdf");
        result.Data!.ContentType.Should().Be("application/pdf");

        using var memoryStream = new MemoryStream();
        await result.Data!.Content.CopyToAsync(memoryStream);
        memoryStream.ToArray().Should().BeEquivalentTo(fileBytes);
    }

    [Fact]
    public async Task GetCvForDownloadAsync_WithUnknownUser_ShouldReturnNotFound()
    {
        // Arrange
        var fakeResumeService = new FakeResumeService();
        var userService = CreateUserService(fakeResumeService);

        // Act
        var result = await userService.GetCvForDownloadAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetCvForDownloadAsync_WithNoCvUploaded_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "no-cv@example.com",
            FirstName = "No",
            LastName = "Cv",
            IsActive = true,
            CvPath = null
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fakeResumeService = new FakeResumeService();
        var userService = CreateUserService(fakeResumeService);

        // Act
        var result = await userService.GetCvForDownloadAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("No CV uploaded");
    }

    [Fact]
    public async Task GetCvForDownloadAsync_WithStorageDesynchronisation_ShouldReturnNotFoundNotInternalError()
    {
        // Arrange: CvPath is set on the user, but the blob no longer exists in storage
        // (GetResumeMetadataAsync fails) - the spec requires 404, not a 500.
        var userId = Guid.NewGuid();
        var cvPath = "https://fakeaccount.blob.core.windows.net/resumes/deleted-file.pdf";
        var user = new User
        {
            Id = userId,
            Email = "desync@example.com",
            FirstName = "De",
            LastName = "Sync",
            IsActive = true,
            CvPath = cvPath
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fakeResumeService = new FakeResumeService(); // Nothing seeded for cvPath => metadata lookup fails
        var userService = CreateUserService(fakeResumeService);

        // Act
        var result = await userService.GetCvForDownloadAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UploadCvAsync_ThenGetCvForDownloadAsync_ShouldRoundTripIdenticalBytes()
    {
        // Arrange - covers acceptance criterion 8 (upload -> download cycle) at the service layer.
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "roundtrip@example.com",
            FirstName = "Round",
            LastName = "Trip",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var fakeResumeService = new FakeResumeService();
        var userService = CreateUserService(fakeResumeService);

        _mockUploadCvValidator.Setup(v => v.ValidateAsync(It.IsAny<UploadCvDto>(), default))
            .ReturnsAsync(new ValidationResult());

        var originalBytes = "This is the content of the CV file."u8.ToArray();
        var formFile = CreateFakeFormFile(originalBytes, "my_cv.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var uploadDto = new UploadCvDto
        {
            CvFile = formFile,
            ReplaceExisting = false,
            ExtractInformation = false
        };

        // Act
        var uploadResult = await userService.UploadCvAsync(userId, uploadDto);
        uploadResult.IsSuccess.Should().BeTrue(uploadResult.Message + " | " + string.Join(",", uploadResult.Errors));

        var downloadResult = await userService.GetCvForDownloadAsync(userId);

        // Assert
        downloadResult.IsSuccess.Should().BeTrue();
        downloadResult.Data!.FileName.Should().Be("my_cv.docx");
        downloadResult.Data!.ContentType.Should()
            .Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        using var memoryStream = new MemoryStream();
        await downloadResult.Data!.Content.CopyToAsync(memoryStream);
        memoryStream.ToArray().Should().BeEquivalentTo(originalBytes);
    }

    private static IFormFile CreateFakeFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "CvFile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private UserService CreateUserService(IResumeService resumeService)
    {
        var mapper = AutoMapperHelper.CreateMapper();
        var userManager = MockHelper.CreateMockUserManager();

        return new UserService(
            _context,
            mapper,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockFilterValidator.Object,
            _mockUploadCvValidator.Object,
            _mockLogger.Object,
            userManager.Object,
            _mockCurrentUserService.Object,
            resumeService
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// In-memory fake of <see cref="IResumeService"/> that stores uploaded bytes keyed by the
    /// blob "path" it hands back, so that upload/download round trips can be asserted without
    /// touching Azure Blob Storage/Azurite. Deliberately does not reproduce
    /// <see cref="XpertSphere.MonolithApi.Services.ResumeService"/>'s internals (out of scope,
    /// per secure-cv-download.md "Hors périmètre").
    /// </summary>
    private class FakeResumeService : IResumeService
    {
        private readonly Dictionary<string, (byte[] Bytes, string FileName, string ContentType)> _store = new();

        public void Seed(string path, byte[] bytes, string fileName, string contentType)
        {
            _store[path] = (bytes, fileName, contentType);
        }

        public Task<ServiceResult<string>> UploadResumeAsync(IFormFile file, Guid userId)
        {
            var path = $"https://fakeaccount.blob.core.windows.net/resumes/{userId}/{file.FileName}";
            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            _store[path] = (memoryStream.ToArray(), file.FileName, file.ContentType);
            return Task.FromResult(ServiceResult<string>.Success(path, "Resume uploaded successfully"));
        }

        public Task<ServiceResult<string>> UpdateResumeAsync(IFormFile file, Guid userId,
            string? existingResumePath = null)
        {
            return UploadResumeAsync(file, userId);
        }

        public Task<ServiceResult> DeleteResumeAsync(string resumePath)
        {
            _store.Remove(resumePath);
            return Task.FromResult(ServiceResult.Success());
        }

        public Task<ServiceResult<Stream>> DownloadResumeAsync(string resumePath)
        {
            if (!_store.TryGetValue(resumePath, out var entry))
            {
                return Task.FromResult(ServiceResult<Stream>.NotFound("Resume not found"));
            }

            Stream stream = new MemoryStream(entry.Bytes);
            return Task.FromResult(ServiceResult<Stream>.Success(stream));
        }

        public Task<ServiceResult<ResumeMetadata>> GetResumeMetadataAsync(string resumePath)
        {
            if (!_store.TryGetValue(resumePath, out var entry))
            {
                return Task.FromResult(ServiceResult<ResumeMetadata>.NotFound("Resume not found"));
            }

            var metadata = new ResumeMetadata
            {
                FileName = entry.FileName,
                Size = entry.Bytes.Length,
                ContentType = entry.ContentType,
                LastModified = DateTime.UtcNow,
                UploadedAt = DateTime.UtcNow
            };
            return Task.FromResult(ServiceResult<ResumeMetadata>.Success(metadata));
        }
    }
}
