using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class ApplicationFilterDtoValidator : AbstractValidator<ApplicationFilterDto>
{
    public ApplicationFilterDtoValidator()
    {
        ConfigureBasicValidation();
        ConfigureBusinessRules();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan("0").WithMessage("Page number must be greater than 0")
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.PageSize)
            .GreaterThan("0").WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo("100").WithMessage("Page size cannot exceed 100")
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.CurrentStatus)
            .IsInEnum().WithMessage("Invalid application status")
            .When(x => x.CurrentStatus.HasValue);

        RuleFor(x => x.MinRating)
            .InclusiveBetween(1, 5).WithMessage("Minimum rating must be between 1 and 5")
            .When(x => x.MinRating.HasValue);

        RuleFor(x => x.MaxRating)
            .InclusiveBetween(1, 5).WithMessage("Maximum rating must be between 1 and 5")
            .When(x => x.MaxRating.HasValue);

        RuleFor(x => x.CandidateName)
            .MaximumLength(200).WithMessage("Candidate name filter cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.CandidateName));

        RuleFor(x => x.JobTitle)
            .MaximumLength(200).WithMessage("Job title filter cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x)
            .Must(x => !x.MinRating.HasValue || !x.MaxRating.HasValue || x.MinRating <= x.MaxRating)
            .WithMessage("Minimum rating cannot be greater than maximum rating in filter")
            .WithName("RatingRange");

        RuleFor(x => x)
            .Must(x => !x.AppliedAfter.HasValue || !x.AppliedBefore.HasValue || x.AppliedAfter <= x.AppliedBefore)
            .WithMessage("Applied after date cannot be greater than applied before date")
            .WithName("AppliedDateRange");

        RuleFor(x => x)
            .Must(x => !x.UpdatedAfter.HasValue || !x.UpdatedBefore.HasValue || x.UpdatedAfter <= x.UpdatedBefore)
            .WithMessage("Updated after date cannot be greater than updated before date")
            .WithName("UpdatedDateRange");
    }
}