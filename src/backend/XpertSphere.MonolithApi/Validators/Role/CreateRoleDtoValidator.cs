using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.Role;

/// <summary>
/// Validator for CreateRoleDto
/// </summary>
public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom du rôle est obligatoire")
            .MaximumLength(100).WithMessage("Le nom du rôle ne peut pas dépasser 100 caractères")
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Le nom du rôle ne peut contenir que des lettres, chiffres, points, underscores et tirets")
            .Must(NotStartWithDot).WithMessage("Le nom du rôle ne peut pas commencer par un point")
            .Must(NotEndWithDot).WithMessage("Le nom du rôle ne peut pas se terminer par un point");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Le nom d'affichage est obligatoire")
            .MaximumLength(150).WithMessage("Le nom d'affichage ne peut pas dépasser 150 caractères")
            .Matches(@"^[a-zA-ZÀ-ÿ0-9\s\-'\.]+$").WithMessage("Le nom d'affichage contient des caractères invalides");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private static bool NotStartWithDot(string name) => !name.StartsWith(".");
    private static bool NotEndWithDot(string name) => !name.EndsWith(".");
}
