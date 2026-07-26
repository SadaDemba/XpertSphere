using FluentValidation;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;

namespace XpertSphere.MonolithApi.Validators.ApplicationStatusHistory;

public class CreateApplicationStatusHistoryDtoValidator : AbstractValidator<CreateApplicationStatusHistoryDto>
{
    public CreateApplicationStatusHistoryDtoValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("L'identifiant de la candidature est obligatoire");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Un statut valide est requis");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Le commentaire est obligatoire")
            .MaximumLength(1000).WithMessage("Le commentaire ne peut pas dépasser 1000 caractères");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La note doit être comprise entre 1 et 5")
            .When(x => x.Rating.HasValue);
    }
}