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
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("Permission ID is required");
    }
}