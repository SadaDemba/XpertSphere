using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Organization;

namespace XpertSphere.MonolithApi.Validators.Organization;

public class CreateOrganizationDtoValidator : AbstractValidator<CreateOrganizationDto>
{
    public CreateOrganizationDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .Length(2, 100).WithMessage("Organization name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&.()]+$").WithMessage("Organization name contains invalid characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Organization code is required.")
            .Length(2, 20).WithMessage("Organization code must be between 2 and 20 characters.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Organization code must contain only uppercase letters, numbers, underscores and dashes.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Industry cannot exceed 100 characters.");

        RuleFor(x => x.Size)
            .IsInEnum().WithMessage("Organization size must be a valid value.");

        
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