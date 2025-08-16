using FluentValidation;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils;

namespace XpertSphere.MonolithApi.Validators.User;

/// <summary>
/// Validator for UploadCvDto
/// </summary>
public class UploadCvDtoValidator : AbstractValidator<UploadCvDto>
{
    private readonly string[] _allowedExtensions = { ".pdf", ".doc", ".docx" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public UploadCvDtoValidator()
    {
        RuleFor(x => x.CvFile)
            .NotNull().WithMessage(Constants.CV_FILE_REQUIRED)
            .Must(BeValidFile).WithMessage(Constants.CV_FILE_INVALID)
            .Must(HaveValidExtension).WithMessage(Constants.CV_FILE_EXTENSION_INVALID)
            .Must(BeWithinSizeLimit).WithMessage(Constants.CV_FILE_SIZE_EXCEEDED);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(Constants.CV_DESCRIPTION_MAX_LENGTH)
            .When(x => !string.IsNullOrEmpty(x.Description));
    }

    private bool BeValidFile(IFormFile file)
    {
        return file != null && file.Length > 0;
    }

    private bool HaveValidExtension(IFormFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FileName))
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    private bool BeWithinSizeLimit(IFormFile file)
    {
        return file != null && file.Length <= MaxFileSizeBytes;
    }
}
