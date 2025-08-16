using FluentValidation;
using XpertSphere.MonolithApi.DTOs.JobOffer;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Validators.JobOffer;

public class CreateJobOfferDtoValidator : AbstractValidator<CreateJobOfferDto>
{
    public CreateJobOfferDtoValidator()
    {
        ConfigureBasicValidation();
        ConfigureBusinessRules();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required")
            .MaximumLength(200).WithMessage("Job title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Job description is required")
            .MinimumLength(50).WithMessage("Job description must be at least 50 characters")
            .MaximumLength(5000).WithMessage("Job description cannot exceed 5000 characters");

        RuleFor(x => x.Requirements)
            .NotEmpty().WithMessage("Job requirements are required")
            .MinimumLength(20).WithMessage("Job requirements must be at least 20 characters")
            .MaximumLength(3000).WithMessage("Job requirements cannot exceed 3000 characters");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.WorkMode)
            .IsInEnum().WithMessage("Invalid work mode");

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Invalid contract type");

        RuleFor(x => x.SalaryCurrency)
            .MaximumLength(10).WithMessage("Salary currency cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.SalaryCurrency));

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required for non-remote positions")
            .When(x => x.WorkMode != WorkMode.FullRemote);
        

        RuleFor(x => x.SalaryMin)
            .GreaterThan(0).WithMessage("Minimum salary must be greater than 0")
            .When(x => x.SalaryMin.HasValue);

        RuleFor(x => x.SalaryMax)
            .GreaterThan(0).WithMessage("Maximum salary must be greater than 0")
            .When(x => x.SalaryMax.HasValue);

        RuleFor(x => x)
            .Must(x => !x.SalaryMin.HasValue || !x.SalaryMax.HasValue || x.SalaryMin <= x.SalaryMax)
            .WithMessage("Minimum salary cannot be greater than maximum salary")
            .WithName("SalaryRange");
    }
}