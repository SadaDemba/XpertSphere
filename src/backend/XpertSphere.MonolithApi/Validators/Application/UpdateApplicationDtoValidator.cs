using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class UpdateApplicationDtoValidator : AbstractValidator<UpdateApplicationDto>
{
    public UpdateApplicationDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.CoverLetter)
            .MaximumLength(2000).WithMessage("Cover letter cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.CoverLetter));

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(1000).WithMessage("Additional notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.AdditionalNotes));

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);
    }
}