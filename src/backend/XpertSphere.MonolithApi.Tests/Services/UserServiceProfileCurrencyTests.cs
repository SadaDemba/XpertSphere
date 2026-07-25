using FluentAssertions;
using FluentValidation;
using Moq;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services;
using XpertSphere.MonolithApi.Tests.Helpers;

namespace XpertSphere.MonolithApi.Tests.Services;

/// <summary>
/// Covers .claude/specifications/configurable-salary-currency.md: <see cref="UserService.UpdateProfileAsync"/>
/// must let a candidate update <c>DesiredSalaryCurrency</c> independently of <c>DesiredSalary</c>, and must
/// never overwrite an existing value when the field is omitted from the request.
/// </summary>
public class UserServiceProfileCurrencyTests : IDisposable
{
    private readonly Mock<IValidator<CreateUserDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateUserDto>> _mockUpdateValidator;
    private readonly Mock<IValidator<UserFilterDto>> _mockFilterValidator;
    private readonly Mock<IValidator<UploadCvDto>> _mockUploadCvValidator;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<UserService>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly XpertSphereDbContext _context;

    public UserServiceProfileCurrencyTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        _mockCreateValidator = new Mock<IValidator<CreateUserDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateUserDto>>();
        _mockFilterValidator = new Mock<IValidator<UserFilterDto>>();
        _mockUploadCvValidator = new Mock<IValidator<UploadCvDto>>();
        _mockLogger = MockHelper.CreateMockLogger<UserService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
    }

    private async Task<User> SeedCandidateAsync(decimal? desiredSalary, Currency? desiredSalaryCurrency)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "candidate@example.com",
            UserName = "candidate@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            IsActive = true,
            DesiredSalary = desiredSalary,
            DesiredSalaryCurrency = desiredSalaryCurrency
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNewCurrency_ShouldUpdateItIndependentlyOfAmount()
    {
        // Arrange
        var user = await SeedCandidateAsync(30000m, Currency.XOF);
        var userService = CreateUserService();

        // Act - only the currency changes, the amount is left untouched
        var result = await userService.UpdateProfileAsync(user.Id,
            new UpdateUserProfileDto { DesiredSalaryCurrency = Currency.EUR });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var reloaded = await _context.Users.FindAsync(user.Id);
        reloaded!.DesiredSalaryCurrency.Should().Be(Currency.EUR);
        reloaded.DesiredSalary.Should().Be(30000m);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithoutCurrencyInRequest_ShouldNotOverwriteExistingValue()
    {
        // Arrange
        var user = await SeedCandidateAsync(30000m, Currency.XOF);
        var userService = CreateUserService();

        // Act - request omits DesiredSalaryCurrency entirely (e.g. only updating LinkedIn profile)
        var result = await userService.UpdateProfileAsync(user.Id,
            new UpdateUserProfileDto { LinkedInProfile = "https://linkedin.com/in/jane-doe" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var reloaded = await _context.Users.FindAsync(user.Id);
        reloaded!.DesiredSalaryCurrency.Should().Be(Currency.XOF);
    }

    private UserService CreateUserService()
    {
        var mapper = AutoMapperHelper.CreateMapper();
        var userManager = MockHelper.CreateMockUserManager();
        var mockResumeService = new Mock<IResumeService>();

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
            mockResumeService.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
