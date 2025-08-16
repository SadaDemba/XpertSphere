using FluentValidation;
using XpertSphere.MonolithApi.DTOs.JobOffer;

namespace XpertSphere.MonolithApi.Validators.JobOffer;

public class JobOfferFilterDtoValidator : AbstractValidator<JobOfferFilterDto>
{
    public JobOfferFilterDtoValidator()
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

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title filter cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location filter cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.WorkMode)
            .IsInEnum().WithMessage("Invalid work mode")
            .When(x => x.WorkMode.HasValue);

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Invalid contract type")
            .When(x => x.ContractType.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid job offer status")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.SalaryMin)
            .GreaterThan(0).WithMessage("Minimum salary filter must be greater than 0")
            .When(x => x.SalaryMin.HasValue);

        RuleFor(x => x.SalaryMax)
            .GreaterThan(0).WithMessage("Maximum salary filter must be greater than 0")
            .When(x => x.SalaryMax.HasValue);
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x)
            .Must(x => !x.SalaryMin.HasValue || !x.SalaryMax.HasValue || x.SalaryMin <= x.SalaryMax)
            .WithMessage("Minimum salary cannot be greater than maximum salary in filter")
            .WithName("SalaryRange");

        RuleFor(x => x)
            .Must(x => !x.PublishedAfter.HasValue || !x.PublishedBefore.HasValue || x.PublishedAfter <= x.PublishedBefore)
            .WithMessage("Published after date cannot be greater than published before date")
            .WithName("PublishedDateRange");

        RuleFor(x => x)
            .Must(x => !x.ExpiresAfter.HasValue || !x.ExpiresBefore.HasValue || x.ExpiresAfter <= x.ExpiresBefore)
            .WithMessage("Expires after date cannot be greater than expires before date")
            .WithName("ExpiresDateRange");
    }
}