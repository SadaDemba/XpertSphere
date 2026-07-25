using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class RegisterUserDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est obligatoire")
            .EmailAddress().WithMessage("Format d'email invalide")
            .MaximumLength(255).WithMessage("L'email ne peut pas dépasser 255 caractères");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est obligatoire")
            .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Les mots de passe ne correspondent pas");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est obligatoire")
            .MaximumLength(100).WithMessage("Le prénom ne peut pas dépasser 100 caractères")
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$")
            .WithMessage("Le prénom ne peut contenir que des lettres, espaces, tirets et apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est obligatoire")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères")
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$")
            .WithMessage("Le nom ne peut contenir que des lettres, espaces, tirets et apostrophes");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]{7,20}$").WithMessage("Format de numéro de téléphone invalide")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Vous devez accepter les conditions d'utilisation");

        RuleFor(x => x.AcceptPrivacyPolicy)
            .Equal(true).WithMessage("Vous devez accepter la politique de confidentialité");
    }
}
