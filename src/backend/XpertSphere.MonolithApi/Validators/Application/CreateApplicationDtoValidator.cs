using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class CreateApplicationDtoValidator : AbstractValidator<CreateApplicationDto>
{
    public CreateApplicationDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.JobOfferId)
            .NotEmpty().WithMessage("Job offer ID is required");

        RuleFor(x => x.CoverLetter)
            .MaximumLength(2000).WithMessage("Cover letter cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.CoverLetter));

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(1000).WithMessage("Additional notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.AdditionalNotes));
    }
}