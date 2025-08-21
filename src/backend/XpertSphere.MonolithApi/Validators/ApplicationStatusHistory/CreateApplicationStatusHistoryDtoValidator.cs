using FluentValidation;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;

namespace XpertSphere.MonolithApi.Validators.ApplicationStatusHistory;

public class CreateApplicationStatusHistoryDtoValidator : AbstractValidator<CreateApplicationStatusHistoryDto>
{
    public CreateApplicationStatusHistoryDtoValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Valid status is required");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required")
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);
    }
}