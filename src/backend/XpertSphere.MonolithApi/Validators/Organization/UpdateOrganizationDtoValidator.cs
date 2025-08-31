using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Organization;

namespace XpertSphere.MonolithApi.Validators.Organization;

public class UpdateOrganizationDtoValidator : AbstractValidator<UpdateOrganizationDto>
{
    public UpdateOrganizationDtoValidator()
    {
        RuleFor(x => x.Name)
            .Length(2, 100).WithMessage("Organization name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&.()]+$").WithMessage("Organization name contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Code)
            .Length(2, 20).WithMessage("Organization code must be between 2 and 20 characters.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Organization code must contain only uppercase letters, numbers, underscores and dashes.")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Industry cannot exceed 100 characters.")
            .When(x => x.Industry != null);

        RuleFor(x => x.Size)
            .IsInEnum().WithMessage("Organization size must be a valid value.")
            .When(x => x.Size.HasValue);

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address!.StreetName)
                .NotEmpty().WithMessage("Street address is required.")
                .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters.");

            RuleFor(x => x.Address!.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

            RuleFor(x => x.Address!.PostalCode)
                .NotEmpty().WithMessage("Postal code is required.")
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

            RuleFor(x => x.Address!.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");
        });

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .Matches(@"^[\+]?[0-9\-\(\)\s]+$").WithMessage("Contact phone contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.Website)
            .Must(BeAValidUrl).WithMessage("Website must be a valid URL.")
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