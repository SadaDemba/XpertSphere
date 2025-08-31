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
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiry date must be in the future")
            .When(x => x.ExpiresAt.HasValue);
    }
}