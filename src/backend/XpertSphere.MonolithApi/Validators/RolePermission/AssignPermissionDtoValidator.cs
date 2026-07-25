using FluentValidation;
using XpertSphere.MonolithApi.DTOs.RolePermission;

namespace XpertSphere.MonolithApi.Validators.RolePermission;

/// <summary>
/// Validator for AssignPermissionDto
/// </summary>
public class AssignPermissionDtoValidator : AbstractValidator<AssignPermissionDto>
{
    public AssignPermissionDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("L'identifiant du rôle est obligatoire");

        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("L'identifiant de la permission est obligatoire");
    }
}