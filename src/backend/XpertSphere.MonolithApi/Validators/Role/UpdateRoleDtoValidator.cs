using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.Role;

/// <summary>
/// Validator for UpdateRoleDto
/// </summary>
public class UpdateRoleDtoValidator : AbstractValidator<UpdateRoleDto>
{
    public UpdateRoleDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Le nom d'affichage est obligatoire")
            .MaximumLength(150).WithMessage("Le nom d'affichage ne peut pas dépasser 150 caractères")
            .Matches(@"^[a-zA-ZÀ-ÿ0-9\s\-'\.]+$").WithMessage("Le nom d'affichage contient des caractères invalides");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("Le statut IsActive est obligatoire");
    }
}
