using FluentValidation;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.User;

/// <summary>
/// Validator for UserFilterDto
/// </summary>
public class UserFilterDtoValidator : AbstractValidator<UserFilterDto>
{
    public UserFilterDtoValidator()
    {
        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage(Constants.DEPARTMENT_FILTER_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.MinExperience)
            .GreaterThanOrEqualTo(0).WithMessage(Constants.MIN_EXPERIENCE_CANNOT_BE_NEGATIVE)
            .LessThanOrEqualTo(50).WithMessage(Constants.MIN_EXPERIENCE_MAX_YEARS)
            .When(x => x.MinExperience.HasValue);

        RuleFor(x => x.MaxExperience)
            .GreaterThanOrEqualTo(0).WithMessage(Constants.MAX_EXPERIENCE_CANNOT_BE_NEGATIVE)
            .LessThanOrEqualTo(50).WithMessage(Constants.MAX_EXPERIENCE_MAX_YEARS)
            .When(x => x.MaxExperience.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinExperience.HasValue || !x.MaxExperience.HasValue || x.MinExperience <= x.MaxExperience)
            .WithMessage(Constants.MIN_EXPERIENCE_GREATER_THAN_MAX);

        RuleFor(x => x.MinSalary)
            .GreaterThan(0).WithMessage(Constants.MIN_SALARY_MUST_BE_POSITIVE)
            .When(x => x.MinSalary.HasValue);

        RuleFor(x => x.MaxSalary)
            .GreaterThan(0).WithMessage(Constants.MAX_SALARY_MUST_BE_POSITIVE)
            .When(x => x.MaxSalary.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinSalary.HasValue || !x.MaxSalary.HasValue || x.MinSalary <= x.MaxSalary)
            .WithMessage(Constants.MIN_SALARY_GREATER_THAN_MAX);

        RuleFor(x => x.Skills)
            .MaximumLength(500).WithMessage(Constants.SKILLS_FILTER_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Skills));

        // Validate base Filter properties
        RuleFor(x => x.PageNumber)
            .Must(BeValidPageNumber).WithMessage(Constants.PAGE_NUMBER_INVALID)
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.PageSize)
            .Must(BeValidPageSize).WithMessage(Constants.PAGE_SIZE_INVALID)
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.SearchTerms)
            .MaximumLength(200).WithMessage(Constants.SEARCH_TERMS_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.SearchTerms));

        RuleFor(x => x.SortBy)
            .MaximumLength(50).WithMessage(Constants.SORT_FIELD_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortDirection)
            .IsInEnum().WithMessage(Constants.SORT_DIRECTION_INVALID);
    }

    private static bool BeValidPageNumber(string pageNumber)
    {
        return int.TryParse(pageNumber, out var result) && result > 0;
    }

    private static bool BeValidPageSize(string pageSize)
    {
        return int.TryParse(pageSize, out var result) && result is > 0 and <= 100;
    }
}
