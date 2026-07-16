using FluentAssertions;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.Validators.Experience;

namespace XpertSphere.MonolithApi.Tests.Validators.Experience;

public class CreateExperienceDtoValidatorTests
{
    private readonly CreateExperienceDtoValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyDescription_ShouldFail()
    {
        // Arrange
        var dto = new CreateExperienceDto
        {
            Title = "Backend Developer",
            Company = "Acme",
            Location = "Paris",
            Date = "2020-2022",
            Description = ""
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Description is required");
    }

    [Fact]
    public async Task Validate_WithWhitespaceDescription_ShouldFail()
    {
        // Arrange
        var dto = new CreateExperienceDto
        {
            Title = "Backend Developer",
            Company = "Acme",
            Location = "Paris",
            Date = "2020-2022",
            Description = "   "
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Description is required");
    }

    [Fact]
    public async Task Validate_WithNullDescription_ShouldFail()
    {
        // Arrange
        var dto = new CreateExperienceDto
        {
            Title = "Backend Developer",
            Company = "Acme",
            Location = "Paris",
            Date = "2020-2022",
            Description = null
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Description is required");
    }

    [Fact]
    public async Task Validate_WithNonEmptyDescription_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateExperienceDto
        {
            Title = "Backend Developer",
            Company = "Acme",
            Location = "Paris",
            Date = "2020-2022",
            Description = "Built and maintained backend services."
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
