using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class UpdateApplicationStatusDtoValidator : AbstractValidator<UpdateApplicationStatusDto>
{
    public UpdateApplicationStatusDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Statut de candidature invalide");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Un commentaire est requis lors de la mise à jour du statut de la candidature")
            .MaximumLength(1000).WithMessage("Le commentaire ne peut pas dépasser 1000 caractères");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La note doit être comprise entre 1 et 5")
            .When(x => x.Rating.HasValue);
    }
}
