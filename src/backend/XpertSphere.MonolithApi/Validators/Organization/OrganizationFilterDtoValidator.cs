using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Organization;

namespace XpertSphere.MonolithApi.Validators.Organization;

public class OrganizationFilterDtoValidator : AbstractValidator<OrganizationFilterDto>
{
    public OrganizationFilterDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo("1").WithMessage("Le numéro de page doit être supérieur ou égal à 1.")
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.PageSize)
            .Must(BeValidPageSize).WithMessage("La taille de page doit être comprise entre 1 et 100.")
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.SearchTerms)
            .MaximumLength(100).WithMessage("Les termes de recherche ne peuvent pas dépasser 100 caractères.")
            .When(x => !string.IsNullOrEmpty(x.SearchTerms));

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .WithMessage("Champ de tri invalide. Champs valides : name, code, industry, contactemail, createdat.")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortDirection)
            .IsInEnum().WithMessage("La direction de tri doit être Ascending ou Descending.");

        RuleFor(x => x.OrganizationSize)
            .IsInEnum().WithMessage("La taille de l'organisation doit être une valeur valide.")
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
