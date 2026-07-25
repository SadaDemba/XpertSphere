using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Organization;

namespace XpertSphere.MonolithApi.Validators.Organization;

public class UpdateOrganizationCurrencyDtoValidator : AbstractValidator<UpdateOrganizationCurrencyDto>
{
    public UpdateOrganizationCurrencyDtoValidator()
    {
        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Invalid currency");
    }
}
