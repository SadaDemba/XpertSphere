using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Permission;

namespace XpertSphere.MonolithApi.Validators.Permission;

/// <summary>
/// Validator for CreatePermissionDto
/// </summary>
public class CreatePermissionDtoValidator : AbstractValidator<CreatePermissionDto>
{
    public CreatePermissionDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom de la permission est obligatoire")
            .MaximumLength(100).WithMessage("Le nom de la permission ne peut pas dépasser 100 caractères")
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Le nom de la permission ne peut contenir que des lettres, chiffres, points, underscores et tirets");

        RuleFor(x => x.Resource)
            .NotEmpty().WithMessage("La ressource est obligatoire")
            .MaximumLength(100).WithMessage("La ressource ne peut pas dépasser 100 caractères")
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("La ressource ne peut contenir que des lettres, chiffres, points, underscores et tirets");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Action de permission invalide");

        RuleFor(x => x.Scope)
            .IsInEnum().WithMessage("Portée de permission invalide")
            .When(x => x.Scope.HasValue);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("La catégorie ne peut pas dépasser 100 caractères")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
