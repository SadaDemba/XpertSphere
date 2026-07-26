using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class UpdateApplicationDtoValidator : AbstractValidator<UpdateApplicationDto>
{
    public UpdateApplicationDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.CoverLetter)
            .MaximumLength(2000).WithMessage("La lettre de motivation ne peut pas dépasser 2000 caractères")
            .When(x => !string.IsNullOrEmpty(x.CoverLetter));

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(1000).WithMessage("Les notes complémentaires ne peuvent pas dépasser 1000 caractères")
            .When(x => !string.IsNullOrEmpty(x.AdditionalNotes));

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La note doit être comprise entre 1 et 5")
            .When(x => x.Rating.HasValue);
    }
}
