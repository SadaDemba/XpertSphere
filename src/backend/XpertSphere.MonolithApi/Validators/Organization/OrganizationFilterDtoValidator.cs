using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Organization;

namespace XpertSphere.MonolithApi.Validators.Organization;

public class OrganizationFilterDtoValidator : AbstractValidator<OrganizationFilterDto>
{
    public OrganizationFilterDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo("1").WithMessage("Page number must be 1 or greater.")
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.PageSize)
            .Must(BeValidPageSize).WithMessage("Page size must be between 1 and 100.")
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.SearchTerms)
            .MaximumLength(100).WithMessage("Search terms cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.SearchTerms));

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("Invalid sort field. Valid fields are: name, code, industry, contactemail, createdat.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortDirection)
            .IsInEnum().WithMessage("Sort direction must be Ascending or Descending.");

        RuleFor(x => x.OrganizationSize)
            .IsInEnum().WithMessage("Organization size must be a valid value.")
            .When(x => x.OrganizationSize != 0);
    }

    private static bool BeValidPageSize(string? pageSize)
    {
        if (string.IsNullOrEmpty(pageSize))
            return true;

        if (!int.TryParse(pageSize, out var size))
            return false;

        return size is >= 1 and <= 100;
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
            return true;

        var validSortFields = new[] { "name", "code", "industry", "contactemail", "createdat" };
        return validSortFields.Contains(sortBy.ToLower());
    }
}