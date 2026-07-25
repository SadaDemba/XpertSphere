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
            .GreaterThan("0").WithMessage("Le numéro de page doit être supérieur à 0")
            .When(x => !string.IsNullOrEmpty(x.PageNumber));

        RuleFor(x => x.PageSize)
            .GreaterThan("0").WithMessage("La taille de page doit être supérieure à 0")
            .LessThanOrEqualTo("100").WithMessage("La taille de page ne peut pas dépasser 100")
            .When(x => !string.IsNullOrEmpty(x.PageSize));

        RuleFor(x => x.CurrentStatus)
            .IsInEnum().WithMessage("Statut de candidature invalide")
            .When(x => x.CurrentStatus.HasValue);

        RuleFor(x => x.MinRating)
            .InclusiveBetween(1, 5).WithMessage("La note minimum doit être comprise entre 1 et 5")
            .When(x => x.MinRating.HasValue);

        RuleFor(x => x.MaxRating)
            .InclusiveBetween(1, 5).WithMessage("La note maximum doit être comprise entre 1 et 5")
            .When(x => x.MaxRating.HasValue);

        RuleFor(x => x.CandidateName)
            .MaximumLength(200).WithMessage("Le filtre de nom de candidat ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.CandidateName));

        RuleFor(x => x.JobTitle)
            .MaximumLength(200).WithMessage("Le filtre d'intitulé de poste ne peut pas dépasser 200 caractères")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));
    }

    private void ConfigureBusinessRules()
    {
        RuleFor(x => x)
            .Must(x => !x.MinRating.HasValue || !x.MaxRating.HasValue || x.MinRating <= x.MaxRating)
            .WithMessage("La note minimum ne peut pas être supérieure à la note maximum dans le filtre")
            .WithName("RatingRange");

        RuleFor(x => x)
            .Must(x => !x.AppliedAfter.HasValue || !x.AppliedBefore.HasValue || x.AppliedAfter <= x.AppliedBefore)
            .WithMessage("La date de candidature 'après' ne peut pas être postérieure à la date 'avant'")
            .WithName("AppliedDateRange");

        RuleFor(x => x)
            .Must(x => !x.UpdatedAfter.HasValue || !x.UpdatedBefore.HasValue || x.UpdatedAfter <= x.UpdatedBefore)
            .WithMessage("La date de mise à jour 'après' ne peut pas être postérieure à la date 'avant'")
            .WithName("UpdatedDateRange");
    }
}
