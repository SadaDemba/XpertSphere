using FluentValidation;
using XpertSphere.MonolithApi.DTOs.Auth;

namespace XpertSphere.MonolithApi.Validators.Auth;

public class RegisterUserDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage("First name can only contain letters, spaces, hyphens and apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters")
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage("Last name can only contain letters, spaces, hyphens and apostrophes");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]{7,20}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("You must accept the terms of service");

        RuleFor(x => x.AcceptPrivacyPolicy)
            .Equal(true).WithMessage("You must accept the privacy policy");
    }
}