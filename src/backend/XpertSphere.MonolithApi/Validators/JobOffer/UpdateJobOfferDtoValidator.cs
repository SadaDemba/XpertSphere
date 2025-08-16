using FluentValidation;
using XpertSphere.MonolithApi.DTOs.JobOffer;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Validators.JobOffer;

public class UpdateJobOfferDtoValidator : AbstractValidator<UpdateJobOfferDto>
{
    public UpdateJobOfferDtoValidator()
    {
        ConfigureBasicValidation();
        ConfigureBusinessRules();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Job title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MinimumLength(50).WithMessage("Job description must be at least 50 characters")
            .MaximumLength(5000).WithMessage("Job description cannot exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Requirements)
            .MinimumLength(20).WithMessage("Job requirements must be at least 20 characters")
            .MaximumLength(3000).WithMessage("Job requirements cannot exceed 3000 characters")
            .When(x => !string.IsNullOrEmpty(x.Requirements));

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.WorkMode)
            .IsInEnum().WithMessage("Invalid work mode")
            .When(x => x.WorkMode.HasValue);

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Invalid contract type")
            .When(x => x.ContractType.HasValue);

        RuleFor(x => x.SalaryCurrency)
            .MaximumLength(10).WithMessage("Salary currency cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.SalaryCurrency));

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Expiration date must be in the future")
            .When(x => x.ExpiresAt.HasValue);
    }

    private void ConfigureBusinessRules()
    {
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