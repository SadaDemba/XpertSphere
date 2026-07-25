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
            .MaximumLength(200).WithMessage("L'intitulé du poste ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MinimumLength(50).WithMessage("La description du poste doit contenir au moins 50 caractères")
            .MaximumLength(5000).WithMessage("La description du poste ne peut pas dépasser 5000 caractères")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Requirements)
            .MinimumLength(20).WithMessage("Les prérequis du poste doivent contenir au moins 20 caractères")
            .MaximumLength(3000).WithMessage("Les prérequis du poste ne peuvent pas dépasser 3000 caractères")
            .When(x => !string.IsNullOrEmpty(x.Requirements));

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("La localisation ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.WorkMode)
            .IsInEnum().WithMessage("Mode de travail invalide")
            .When(x => x.WorkMode.HasValue);

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Type de contrat invalide")
            .When(x => x.ContractType.HasValue);

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("La date d'expiration doit être dans le futur")
            .When(x => x.ExpiresAt.HasValue);
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x.SalaryMin)
            .GreaterThan(0).WithMessage("Le salaire minimum doit être supérieur à 0")
            .When(x => x.SalaryMin.HasValue);

        RuleFor(x => x.SalaryMax)
            .GreaterThan(0).WithMessage("Le salaire maximum doit être supérieur à 0")
            .When(x => x.SalaryMax.HasValue);

        RuleFor(x => x)
            .Must(x => !x.SalaryMin.HasValue || !x.SalaryMax.HasValue || x.SalaryMin <= x.SalaryMax)
            .WithMessage("Le salaire minimum ne peut pas être supérieur au salaire maximum")
            .WithName("SalaryRange");
    }
}
