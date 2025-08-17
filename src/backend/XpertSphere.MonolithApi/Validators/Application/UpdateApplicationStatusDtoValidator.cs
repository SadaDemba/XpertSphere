using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class UpdateApplicationStatusDtoValidator : AbstractValidator<UpdateApplicationStatusDto>
{
    public UpdateApplicationStatusDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid application status");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required when updating application status")
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);
    }
}