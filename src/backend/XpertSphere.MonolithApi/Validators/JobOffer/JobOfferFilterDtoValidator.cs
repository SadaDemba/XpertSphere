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
            .GreaterThan("0").WithMessage("Le numéro de page doit être supérieur à 0")
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.PageSize)
            .GreaterThan("0").WithMessage("La taille de page doit être supérieure à 0")
            .LessThanOrEqualTo("100").WithMessage("La taille de page ne peut pas dépasser 100")
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Le filtre d'intitulé ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Le filtre de localisation ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.WorkMode)
            .IsInEnum().WithMessage("Mode de travail invalide")
            .When(x => x.WorkMode.HasValue);

        RuleFor(x => x.ContractType)
            .IsInEnum().WithMessage("Type de contrat invalide")
            .When(x => x.ContractType.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Statut d'offre d'emploi invalide")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.SalaryMin)
            .GreaterThan(0).WithMessage("Le filtre de salaire minimum doit être supérieur à 0")
            .When(x => x.SalaryMin.HasValue);

        RuleFor(x => x.SalaryMax)
            .GreaterThan(0).WithMessage("Le filtre de salaire maximum doit être supérieur à 0")
            .When(x => x.SalaryMax.HasValue);
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x)
            .Must(x => !x.SalaryMin.HasValue || !x.SalaryMax.HasValue || x.SalaryMin <= x.SalaryMax)
            .WithMessage("Le salaire minimum ne peut pas être supérieur au salaire maximum dans le filtre")
            .WithName("SalaryRange");

        RuleFor(x => x)
            .Must(x => !x.PublishedAfter.HasValue || !x.PublishedBefore.HasValue ||
                       x.PublishedAfter <= x.PublishedBefore)
            .WithMessage("La date de publication 'après' ne peut pas être postérieure à la date 'avant'")
            .WithName("PublishedDateRange");

        RuleFor(x => x)
            .Must(x => !x.ExpiresAfter.HasValue || !x.ExpiresBefore.HasValue || x.ExpiresAfter <= x.ExpiresBefore)
            .WithMessage("La date d'expiration 'après' ne peut pas être postérieure à la date 'avant'")
            .WithName("ExpiresDateRange");
    }
}
