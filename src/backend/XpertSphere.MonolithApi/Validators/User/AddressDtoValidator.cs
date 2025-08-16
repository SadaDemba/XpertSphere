using FluentValidation;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.User;

/// <summary>
/// Validator for AddressDto (handles nullable scenarios)
/// </summary>
public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.StreetNumber)
            .MaximumLength(10).WithMessage(Constants.STREET_NUMBER_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.StreetNumber));

        RuleFor(x => x.StreetName)
            .MaximumLength(255).WithMessage(Constants.STREET_NAME_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.StreetName));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage(Constants.CITY_MAX_LENGTH)
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage(Constants.CITY_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage(Constants.POSTAL_CODE_MAX_LENGTH)
            .Matches(@"^[0-9A-Za-z\s\-]+$").WithMessage(Constants.POSTAL_CODE_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Region)
            .MaximumLength(100).WithMessage(Constants.REGION_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Region));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage(Constants.COUNTRY_MAX_LENGTH)
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage(Constants.COUNTRY_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.Country));

        RuleFor(x => x.AddressLine2)
            .MaximumLength(255).WithMessage(Constants.ADDRESS_LINE2_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));
    }
}

/// <summary>
/// Validator for nullable AddressDto
/// </summary>
public class NullableAddressDtoValidator : AbstractValidator<AddressDto?>
{
    public NullableAddressDtoValidator()
    {
        RuleFor(x => x)
            .SetValidator(new AddressDtoValidator()!)
            .When(x => x != null);
    }
}
