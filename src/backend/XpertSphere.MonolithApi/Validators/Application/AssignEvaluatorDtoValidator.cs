using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class AssignUserDtoValidator : AbstractValidator<AssignUserDto>
{
    public AssignUserDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.AssignmentType)
            .IsInEnum().WithMessage("Invalid assignment type");
    }
}