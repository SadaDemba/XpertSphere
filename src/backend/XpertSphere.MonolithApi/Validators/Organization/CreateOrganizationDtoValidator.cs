using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Organization;

namespace XpertSphere.MonolithApi.Validators.Organization;

public class CreateOrganizationDtoValidator : AbstractValidator<CreateOrganizationDto>
{
    public CreateOrganizationDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom de l'organisation est obligatoire.")
            .Length(2, 100).WithMessage("Le nom de l'organisation doit contenir entre 2 et 100 caractères.")
            .Matches(@"^[a-zA-Z0-9\s\-&.()]+$").WithMessage("Le nom de l'organisation contient des caractères invalides.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Le code de l'organisation est obligatoire.")
            .Length(2, 20).WithMessage("Le code de l'organisation doit contenir entre 2 et 20 caractères.")
            .Matches(@"^[A-Z0-9_-]+$")
            .WithMessage("Le code de l'organisation ne peut contenir que des majuscules, des chiffres, des underscores et des tirets.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne peut pas dépasser 500 caractères.");

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Le secteur d'activité ne peut pas dépasser 100 caractères.");

        RuleFor(x => x.Size)
            .IsInEnum().WithMessage("La taille de l'organisation doit être une valeur valide.");


        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("L'email de contact doit être une adresse email valide.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .Matches(@"^[\+]?[0-9\-\(\)\s]+$").WithMessage("Le téléphone de contact contient des caractères invalides.")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.Website)
            .Must(BeAValidUrl).WithMessage("Le site web doit être une URL valide.")
            .When(x => !string.IsNullOrEmpty(x.Website));
    }

    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
