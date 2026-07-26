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
            .NotEmpty().WithMessage("L'intitulé du poste est obligatoire")
            .MaximumLength(200).WithMessage("L'intitulé du poste ne peut pas dépasser 200 caractères");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La description du poste est obligatoire")
            .MaximumLength(5000).WithMessage("La description du poste ne peut pas dépasser 5000 caractères");

        RuleFor(x => x.Requirements)
            .NotEmpty().WithMessage("Les prérequis du poste sont obligatoires")
            .MaximumLength(3000).WithMessage("Les prérequis du poste ne peuvent pas dépasser 3000 caractères");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("La localisation ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.WorkMode)
            .IsInEnum().WithMessage("Mode de travail invalide");

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Type de contrat invalide");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("La date d'expiration doit être dans le futur")
            .When(x => x.ExpiresAt.HasValue);
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La localisation est obligatoire pour les postes non intégralement en télétravail")
            .When(x => x.WorkMode != WorkMode.FullRemote);


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
