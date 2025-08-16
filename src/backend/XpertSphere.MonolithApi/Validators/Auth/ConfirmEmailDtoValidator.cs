using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
{
    public ConfirmEmailDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Confirmation token is required");
    }
}