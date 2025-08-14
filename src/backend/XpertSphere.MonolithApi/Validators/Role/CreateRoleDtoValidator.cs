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
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(100).WithMessage("Role name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Role name can only contain letters, numbers, dots, underscores, and hyphens")
            .Must(NotStartWithDot).WithMessage("Role name cannot start with a dot")
            .Must(NotEndWithDot).WithMessage("Role name cannot end with a dot");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(150).WithMessage("Display name cannot exceed 150 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ0-9\s\-'\.]+$").WithMessage("Display name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private static bool NotStartWithDot(string name) => !name.StartsWith(".");
    private static bool NotEndWithDot(string name) => !name.EndsWith(".");
}