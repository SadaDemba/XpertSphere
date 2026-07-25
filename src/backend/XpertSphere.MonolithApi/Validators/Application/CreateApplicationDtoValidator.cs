using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class CreateApplicationDtoValidator : AbstractValidator<CreateApplicationDto>
{
    public CreateApplicationDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.JobOfferId)
            .NotEmpty().WithMessage("L'identifiant de l'offre d'emploi est obligatoire");

        RuleFor(x => x.CoverLetter)
            .MaximumLength(2000).WithMessage("La lettre de motivation ne peut pas dépasser 2000 caractères")
            .When(x => !string.IsNullOrEmpty(x.CoverLetter));

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(1000).WithMessage("Les notes complémentaires ne peuvent pas dépasser 1000 caractères")
            .When(x => !string.IsNullOrEmpty(x.AdditionalNotes));
    }
}
