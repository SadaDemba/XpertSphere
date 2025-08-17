using FluentValidation;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;

namespace XpertSphere.MonolithApi.Validators.ApplicationStatusHistory;

public class AddStatusChangeDtoValidator : AbstractValidator<AddStatusChangeDto>
{
    public AddStatusChangeDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid application status");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required when adding status change")
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);

        RuleFor(x => x.UpdatedByUserId)
            .NotEmpty().WithMessage("Updated by user ID is required");
    }
}