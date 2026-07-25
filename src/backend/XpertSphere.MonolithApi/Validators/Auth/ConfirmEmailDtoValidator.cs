using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
{
    public ConfirmEmailDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est obligatoire")
            .EmailAddress().WithMessage("Format d'email invalide");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Le jeton de confirmation est obligatoire");
    }
}
