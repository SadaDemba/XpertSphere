using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class ResendConfirmationDtoValidator : AbstractValidator<ResendConfirmationDto>
{
    public ResendConfirmationDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est obligatoire")
            .EmailAddress().WithMessage("Format d'email invalide");
    }
}
