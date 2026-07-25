using FluentValidation;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;

namespace XpertSphere.MonolithApi.Validators.ApplicationStatusHistory;

public class AddStatusChangeDtoValidator : AbstractValidator<AddStatusChangeDto>
{
    public AddStatusChangeDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("L'identifiant de la candidature est obligatoire");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Statut de candidature invalide");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Un commentaire est requis lors de l'ajout d'un changement de statut")
            .MaximumLength(1000).WithMessage("Le commentaire ne peut pas dépasser 1000 caractères");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("La note doit être comprise entre 1 et 5")
            .When(x => x.Rating.HasValue);

        RuleFor(x => x.UpdatedByUserId)
            .NotEmpty().WithMessage("L'identifiant de l'utilisateur ayant effectué la mise à jour est obligatoire");
    }
}