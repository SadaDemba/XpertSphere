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
            .NotEmpty().WithMessage("Permission name is required")
            .MaximumLength(100).WithMessage("Permission name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Permission name can only contain letters, numbers, dots, underscores, and hyphens");

        RuleFor(x => x.Resource)
            .NotEmpty().WithMessage("Resource is required")
            .MaximumLength(100).WithMessage("Resource cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Resource can only contain letters, numbers, dots, underscores, and hyphens");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Invalid permission action");

        RuleFor(x => x.Scope)
            .IsInEnum().WithMessage("Invalid permission scope")
            .When(x => x.Scope.HasValue);

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}