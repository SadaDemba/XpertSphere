using FluentValidation;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;

namespace XpertSphere.MonolithApi.Validators.Experience;

/// <summary>
/// Validator for CreateExperienceDto
/// </summary>
public class CreateExperienceDtoValidator : AbstractValidator<CreateExperienceDto>
{
    public CreateExperienceDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
    }
}
