using FluentValidation;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.User;

/// <summary>
/// Validator for CreateUserDto
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        ConfigureBasicValidation();
        ConfigureOrganizationSpecificValidation();
        ConfigureAddressValidation();
        ConfigureProfessionalValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(Constants.FIRST_NAME_REQUIRED)
            .MaximumLength(100).WithMessage(Constants.FIRST_NAME_MAX_LENGTH)
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage(Constants.FIRST_NAME_INVALID_FORMAT);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(Constants.LAST_NAME_REQUIRED)
            .MaximumLength(100).WithMessage(Constants.LAST_NAME_MAX_LENGTH)
            .Matches(@"^[a-zA-ZÀ-ÿ\s\-']+$").WithMessage(Constants.LAST_NAME_INVALID_FORMAT);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(Constants.EMAIL_REQUIRED)
            .EmailAddress().WithMessage(Constants.EMAIL_INVALID_FORMAT)
            .MaximumLength(255).WithMessage(Constants.EMAIL_MAX_LENGTH);

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[\+]?[0-9\s\-\(\)\.]{7,20}$").WithMessage(Constants.PHONE_NUMBER_INVALID_FORMAT)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }

    private void ConfigureOrganizationSpecificValidation()
    {
        // Employee ID is required when OrganizationId is present
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage(Constants.EMPLOYEE_ID_REQUIRED_FOR_ORGANIZATIONAL_USERS)
            .MaximumLength(50).WithMessage(Constants.EMPLOYEE_ID_MAX_LENGTH)
            .When(x => x.OrganizationId.HasValue);

        // Employee ID validation for optional cases
        RuleFor(x => x.EmployeeId)
            .MaximumLength(50).WithMessage(Constants.EMPLOYEE_ID_MAX_LENGTH)
            .When(x => !x.OrganizationId.HasValue && !string.IsNullOrEmpty(x.EmployeeId));

        // Department validation for internal users
        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage(Constants.DEPARTMENT_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Department));

        // Hire date should not be in the future for internal users
        RuleFor(x => x.HireDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage(Constants.HIRE_DATE_CANNOT_BE_FUTURE)
            .When(x => x.HireDate.HasValue);
    }

    private void ConfigureProfessionalValidation()
    {
        // LinkedIn profile validation
        RuleFor(x => x.LinkedInProfile)
            .Must(BeValidLinkedInUrl).WithMessage(Constants.LINKEDIN_PROFILE_INVALID)
            .When(x => !string.IsNullOrEmpty(x.LinkedInProfile));

        // Experience validation
        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage(Constants.EXPERIENCE_CANNOT_BE_NEGATIVE)
            .LessThanOrEqualTo(50).WithMessage(Constants.EXPERIENCE_MAX_YEARS)
            .When(x => x.YearsOfExperience.HasValue);

        // Salary validation
        RuleFor(x => x.DesiredSalary)
            .GreaterThan(0).WithMessage(Constants.DESIRED_SALARY_MUST_BE_POSITIVE)
            .LessThan(1000000).WithMessage(Constants.DESIRED_SALARY_UNREALISTIC)
            .When(x => x.DesiredSalary.HasValue);

        // Availability validation
        RuleFor(x => x.Availability)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage(Constants.AVAILABILITY_CANNOT_BE_PAST)
            .When(x => x.Availability.HasValue);

        // Skills validation
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
