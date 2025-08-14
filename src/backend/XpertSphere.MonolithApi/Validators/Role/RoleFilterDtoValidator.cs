using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Role;

namespace XpertSphere.MonolithApi.Validators.Role;

/// <summary>
/// Validator for RoleFilterDto
/// </summary>
public class RoleFilterDtoValidator : AbstractValidator<RoleFilterDto>
{
    public RoleFilterDtoValidator()
    {
        ConfigureFilterValidation();
    }

    private void ConfigureFilterValidation()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Role name filter cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.DisplayName)
            .MaximumLength(150).WithMessage("Display name filter cannot exceed 150 characters")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description filter cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.SearchTerms)
            .MaximumLength(255).WithMessage("Search terms cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchTerms));

        RuleFor(x => x.PageSize)
            .Must(BeValidPageSize).WithMessage("Page size must be between 1 and 100")
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.PageNumber)
            .Must(BeValidPageNumber).WithMessage("Page number must be greater than 0")
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("Invalid sort field. Valid fields are: name, displayname, createdat")
            .When(x => !string.IsNullOrEmpty(x.SortBy));
    }

    private static bool BeValidPageSize(string? pageSize)
    {
        if (string.IsNullOrEmpty(pageSize)) return true;
        if (!int.TryParse(pageSize, out int size)) return false;
        return size is >= 1 and <= 100;
    }

    private static bool BeValidPageNumber(string? pageNumber)
    {
        if (string.IsNullOrEmpty(pageNumber)) return true;
        if (!int.TryParse(pageNumber, out int number)) return false;
        return number >= 1;
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy)) return true;
        var validFields = new[] { "name", "displayname", "createdat" };
        return validFields.Contains(sortBy.ToLower());
    }
}