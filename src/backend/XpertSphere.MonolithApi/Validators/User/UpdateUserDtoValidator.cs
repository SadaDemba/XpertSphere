using FluentValidation;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.User;

/// <summary>
/// Validator for UpdateUserDto
/// </summary>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        ConfigureBasicValidation();
        ConfigureProfessionalValidation();
        ConfigureAddressValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage(Constants.FIRST_NAME_MAX_LENGTH)
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage(Constants.FIRST_NAME_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage(Constants.LAST_NAME_MAX_LENGTH)
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage(Constants.LAST_NAME_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(Constants.EMAIL_INVALID_FORMAT)
            .MaximumLength(255).WithMessage(Constants.EMAIL_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]{7,20}$").WithMessage(Constants.PHONE_NUMBER_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));


        RuleFor(x => x.EmployeeId)
            .MaximumLength(50).WithMessage(Constants.EMPLOYEE_ID_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.EmployeeId));

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage(Constants.DEPARTMENT_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.HireDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage(Constants.HIRE_DATE_CANNOT_BE_FUTURE)
            .When(x => x.HireDate.HasValue);
    }

    private void ConfigureProfessionalValidation()
    {
        RuleFor(x => x.LinkedInProfile)
            .Must(BeValidLinkedInUrl).WithMessage(Constants.LINKEDIN_PROFILE_INVALID)
            .When(x => !string.IsNullOrEmpty(x.LinkedInProfile));

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage(Constants.EXPERIENCE_CANNOT_BE_NEGATIVE)
            .LessThanOrEqualTo(50).WithMessage(Constants.EXPERIENCE_MAX_YEARS)
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.DesiredSalary)
            .GreaterThan(0).WithMessage(Constants.DESIRED_SALARY_MUST_BE_POSITIVE)
            .LessThan(1000000).WithMessage(Constants.DESIRED_SALARY_UNREALISTIC)
            .When(x => x.DesiredSalary.HasValue);

        RuleFor(x => x.Availability)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage(Constants.AVAILABILITY_CANNOT_BE_PAST)
            .When(x => x.Availability.HasValue);

        RuleFor(x => x.Skills)
            .MaximumLength(1000).WithMessage(Constants.SKILLS_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Skills));
    }

    private void ConfigureAddressValidation()
    {
        RuleFor(x => x.Address)
            .SetValidator(new NullableAddressDtoValidator());
    }

    private static bool BeValidLinkedInUrl(string? linkedInUrl)
    {
        if (string.IsNullOrEmpty(linkedInUrl))
            return true;

        return linkedInUrl.Contains("linkedin.com/in/") || 
               linkedInUrl.Contains("linkedin.com/pub/") ||
               linkedInUrl.StartsWith("https://linkedin.com/") ||
               linkedInUrl.StartsWith("https://www.linkedin.com/");
    }
}
