using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est obligatoire")
            .EmailAddress().WithMessage("Format d'email invalide");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Le jeton de rafraîchissement est obligatoire");
    }
}
