using FluentValidation;
using XpertSphere.MonolithApi.DTOs.UserRole;

namespace XpertSphere.MonolithApi.Validators.UserRole;

/// <summary>
/// Validator for AssignRoleDto
/// </summary>
public class AssignRoleDtoValidator : AbstractValidator<AssignRoleDto>
{
    public AssignRoleDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("L'identifiant de l'utilisateur est obligatoire");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("L'identifiant du rôle est obligatoire");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("La date d'expiration doit être dans le futur")
            .When(x => x.ExpiresAt.HasValue);
    }
}