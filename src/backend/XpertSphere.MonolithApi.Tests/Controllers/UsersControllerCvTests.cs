using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using XpertSphere.MonolithApi.Controllers;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Tests.Controllers;

/// <summary>
/// Tests for <see cref="UsersController.DownloadCv"/> and the authorization wiring described in
/// secure-cv-download.md.
///
/// Note on scope: criteria 2, 3, 4 and 7 of the specification describe HTTP-level outcomes (401
/// with no auth header, 403 for a candidate requesting someone else's CV, 200 for an
/// organization-role user) that are produced by ASP.NET Core's authorization middleware
/// evaluating the "CandidateOwnDataAccess" policy - not by this controller action's own code, and
/// not by <see cref="UserService"/>. Exercising that middleware would require a
/// WebApplicationFactory-based integration harness; this repository has no such harness today
/// (Program.cs runs a real EF Core migration/seed step unconditionally on startup, which would
/// need to be guarded for a test environment - a change considered out of scope for this
/// feature). The tests below instead verify the two facts that determine those HTTP outcomes and
/// that are actually under this feature's control: the action attribute wiring (regression guard
/// for criterion 7 in particular - UploadCv had no [Authorize] before this feature) and the
/// action's own mapping of a ServiceResult to an HTTP response.
/// </summary>
public class UsersControllerCvTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UsersController _controller;

    public UsersControllerCvTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UsersController(_mockUserService.Object);
    }

    [Fact]
    public async Task DownloadCv_WithExistingCv_ShouldReturnFileResultWithContentTypeAndFileName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var content = new MemoryStream([1, 2, 3]);
        var cvDownloadResult = new CvDownloadResult
        {
            Content = content,
            FileName = "cv.pdf",
            ContentType = "application/pdf"
        };
        _mockUserService.Setup(x => x.GetCvForDownloadAsync(userId))
            .ReturnsAsync(ServiceResult<CvDownloadResult>.Success(cvDownloadResult));

        // Act
        var result = await _controller.DownloadCv(userId);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("cv.pdf");
        fileResult.FileStream.Should().BeSameAs(content);
    }

    [Fact]
    public async Task DownloadCv_WithNoCvUploaded_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserService.Setup(x => x.GetCvForDownloadAsync(userId))
            .ReturnsAsync(ServiceResult<CvDownloadResult>.NotFound("No CV uploaded for this user"));

        // Act
        var result = await _controller.DownloadCv(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DownloadCv_WithUnknownUser_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserService.Setup(x => x.GetCvForDownloadAsync(userId))
            .ReturnsAsync(ServiceResult<CvDownloadResult>.NotFound($"User with ID {userId} not found"));

        // Act
        var result = await _controller.DownloadCv(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void DownloadCv_ShouldBeProtectedByCandidateOwnDataAccessPolicy()
    {
        // Regression guard: the endpoint must remain authenticated/authorized, matching the
        // policy used by GET /api/users/{id} and GET /api/users/{id}/profile (spec §2).
        var method = typeof(UsersController).GetMethod(nameof(UsersController.DownloadCv));
        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Policy.Should().Be("CandidateOwnDataAccess");
    }

    [Fact]
    public void UploadCv_ShouldBeProtectedByCandidateOwnDataAccessPolicy()
    {
        // Regression guard for the connected fix in this same feature (spec §3): before this
        // feature, POST /api/users/{id}/cv had no [Authorize] at all, so any unauthenticated
        // request could upload a CV for any user (criterion 7).
        var method = typeof(UsersController).GetMethod(nameof(UsersController.UploadCv));
        var authorizeAttribute = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Policy.Should().Be("CandidateOwnDataAccess");
    }
}
