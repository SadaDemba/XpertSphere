using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Application;

namespace XpertSphere.MonolithApi.Validators.Application;

public class AssignUserDtoValidator : AbstractValidator<AssignUserDto>
{
    public AssignUserDtoValidator()
    {
        ConfigureBasicValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("L'identifiant de la candidature est obligatoire");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("L'identifiant de l'utilisateur est obligatoire");

        RuleFor(x => x.AssignmentType)
            .IsInEnum().WithMessage("Type d'affectation invalide");
    }
}
