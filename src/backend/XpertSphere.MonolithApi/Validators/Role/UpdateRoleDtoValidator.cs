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
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ0-9\s\-'\.]+$").WithMessage("Display name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive status is required");
    }
}