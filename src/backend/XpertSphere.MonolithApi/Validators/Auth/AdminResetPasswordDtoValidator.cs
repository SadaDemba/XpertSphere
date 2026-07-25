using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class AdminResetPasswordDtoValidator : AbstractValidator<AdminResetPasswordDto>
{
    public AdminResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("L'email est obligatoire")
            .EmailAddress()
            .WithMessage("Format d'email invalide");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Le nouveau mot de passe est obligatoire")
            .MinimumLength(6)
            .WithMessage("Le mot de passe doit contenir au moins 6 caractères")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("Le mot de passe doit contenir au moins une minuscule, une majuscule et un chiffre");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("La confirmation du mot de passe est obligatoire")
            .Equal(x => x.NewPassword)
            .WithMessage("La confirmation du mot de passe doit correspondre au nouveau mot de passe");
    }
}
