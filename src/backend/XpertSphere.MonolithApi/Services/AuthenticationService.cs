using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using XpertSphere.MonolithApi.Config;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils;
using System.Web;

namespace XpertSphere.MonolithApi.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly EntraIdSettings _entraIdSettings;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RefreshTokenDto> _refreshTokenValidator;
    private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
    private readonly IValidator<ConfirmEmailDto> _confirmEmailValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
    private readonly IValidator<AdminResetPasswordDto> _adminResetPasswordValidator;
    private readonly IValidator<ResendConfirmationDto> _resendConfirmationValidator;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private readonly ITrainingService _trainingService;
    private readonly IExperienceService _experienceService;
    private readonly IResumeService _resumeService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly FrontendSettings _frontendSettings;
    private readonly XpertSphereDbContext _context;

    private const string ResendConfirmationGenericMessage =
        "Si un compte existe pour cet email et n'est pas encore confirmé, un nouvel email d'activation vient d'être envoyé.";

    // Check if Entra ID should be used (from environment variable)
    private bool ShouldUseEntraId =>
        !_environment.IsDevelopment() &&
        Environment.GetEnvironmentVariable("USE_ENTRA_ID")?.ToLower() == "true";

    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EntraIdSettings> entraIdSettings,
        IMapper mapper,
        ILogger<AuthenticationService> logger,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<RefreshTokenDto> refreshTokenValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator,
        IValidator<ConfirmEmailDto> confirmEmailValidator,
        IValidator<ForgotPasswordDto> forgotPasswordValidator,
        IValidator<AdminResetPasswordDto> adminResetPasswordValidator,
        IValidator<ResendConfirmationDto> resendConfirmationValidator,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        IUserService userService,
        IResumeService resumeService,
        ITrainingService trainingService,
        IExperienceService experienceService,
        IEmailNotificationService emailNotificationService,
        IOptions<FrontendSettings> frontendSettings,
        XpertSphereDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
        _entraIdSettings = entraIdSettings.Value;
        _mapper = mapper;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _confirmEmailValidator = confirmEmailValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _adminResetPasswordValidator = adminResetPasswordValidator;
        _resendConfirmationValidator = resendConfirmationValidator;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
        _resumeService = resumeService;
        _trainingService = trainingService;
        _experienceService = experienceService;
        _emailNotificationService = emailNotificationService;
        _frontendSettings = frontendSettings.Value;
        _context = context;
    }

    public async Task<AuthResult> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var validationResult = await _registerValidator.ValidateAsync(registerDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(errors);
            }

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return AuthResult.Conflict("Un utilisateur avec cet email existe déjà");
            }

            // Check if we should redirect to Entra ID
            if (ShouldUseEntraId)

            {
                // For candidates (users without organization), offer Entra ID B2C registration
                var entraIdSignUpUrl = GenerateEntraIdSignUpUrl(registerDto.ReturnUrl ?? "/profile");
                if (!string.IsNullOrEmpty(entraIdSignUpUrl))
                {
                    _logger.LogInformation("Redirecting candidate {Email} to Entra ID B2C registration",
                        registerDto.Email);
                    return AuthResult.Success("Terminez votre inscription avec votre fournisseur d'identité préféré",
                        entraIdSignUpUrl);
                }
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.PhoneNumber,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };


            user.CalculateProfileCompletion();

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return AuthResult.Failure($"Échec de l'inscription : {errors}");
            }


            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);

            // Generate email confirmation token
            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.EmailConfirmationToken = emailConfirmationToken;
            var authDto = _mapper.Map<AuthResponseDto>(user);

            return AuthResult.SuccessWithUser(authDto,
                "Inscription réussie. Veuillez vérifier votre email pour confirmer votre compte.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration for {Email}", registerDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors de l'inscription");
        }
    }

    public async Task<AuthResult> RegisterCandidateAsync(RegisterCandidateDto registerDto, IFormFile? resumeFile = null)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Basic validation
                if (!registerDto.AcceptTerms || !registerDto.AcceptPrivacyPolicy)
                {
                    return AuthResult.ValidationError(["Vous devez accepter les conditions d'utilisation et la politique de confidentialité"]);
                }

                // Each experience, once added, must have a non-empty description
                if (registerDto.Experiences?.Count > 0)
                {
                    var experienceErrors = new List<string>();
                    for (var i = 0; i < registerDto.Experiences.Count; i++)
                    {
                        var experience = registerDto.Experiences[i];
                        if (string.IsNullOrWhiteSpace(experience.Description))
                        {
                            experienceErrors.Add(string.IsNullOrWhiteSpace(experience.Title)
                                ? $"L'expérience n°{i + 1} n'a pas de description."
                                : $"L'expérience n°{i + 1} (« {experience.Title} ») n'a pas de description.");
                        }
                    }

                    if (experienceErrors.Count > 0)
                    {
                        return AuthResult.ValidationError(experienceErrors);
                    }
                }

                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return AuthResult.Conflict("Un utilisateur avec cet email existe déjà");
                }

                // Create user using UserManager directly
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    EmailConfirmed = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrganizationId = null,

                    // Address
                    Address = new Address
                    {
                        StreetNumber = registerDto.StreetNumber,
                        StreetName = registerDto.Street,
                        City = registerDto.City,
                        PostalCode = registerDto.PostalCode,
                        Region = registerDto.Region,
                        Country = registerDto.Country,
                        AddressLine2 = registerDto.AddressLine2
                    },

                    // Professional info
                    Skills = registerDto.Skills,
                    YearsOfExperience = registerDto.YearsOfExperience,
                    DesiredSalary = registerDto.DesiredSalary,
                    // Repli explicite si la devise n'a pas été envoyée alors qu'un montant l'a été
                    // (client direct sur l'API n'ayant pas mis à jour son contrat) : cohérent avec
                    // le backfill des comptes candidats existants (Currency.XOF). Si aucun montant
                    // n'est renseigné, la devise n'a pas de sens et reste null.
                    DesiredSalaryCurrency = registerDto.DesiredSalaryCurrency
                        ?? (registerDto.DesiredSalary.HasValue ? Currency.XOF : null),
                    Availability = registerDto.Availability,
                    LinkedInProfile = registerDto.LinkedInProfile,

                    // Communication preferences
                    EmailNotificationsEnabled = registerDto.EmailNotificationsEnabled,
                    SmsNotificationsEnabled = registerDto.SmsNotificationsEnabled,
                    PreferredLanguage = registerDto.PreferredLanguage,
                    TimeZone = registerDto.TimeZone,

                    // Consent
                    ConsentGivenAt = registerDto.ConsentGivenAt ?? DateTime.UtcNow,
                    ExternalId = registerDto.ExternalId
                };

                // Create user with password
                var createResult = await _userManager.CreateAsync(user, registerDto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return AuthResult.Failure($"Échec de la création de l'utilisateur : {errors}");
                }

                // Add trainings if provided
                if (registerDto.Trainings?.Count > 0)
                {
                    foreach (var training in registerDto.Trainings)
                    {
                        training.UserId = user.Id;
                        await _trainingService.CreateTrainingAsync(training);
                    }
                }

                // Add experiences if provided
                if (registerDto.Experiences?.Count > 0)
                {
                    foreach (var experience in registerDto.Experiences)
                    {
                        experience.UserId = user.Id;
                        await _experienceService.CreateExperienceAsync(experience);
                    }
                }

                // Upload resume within transaction if provided
                if (resumeFile != null)
                {
                    var resumeUploadResult = await _resumeService.UploadResumeAsync(resumeFile, user.Id);
                    if (resumeUploadResult.IsSuccess)
                    {
                        user.CvPath = resumeUploadResult.Data;
                        _context.Users.Update(user);
                    }
                }

                // Assign candidate role
                var candidateRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Candidate.Name);
                if (candidateRole != null)
                {
                    var userRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = candidateRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                }

                // Calculate profile completion
                user.CalculateProfileCompletion();
                await _userManager.UpdateAsync(user);

                // Save all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Candidate {Email} registered successfully with complete profile",
                    registerDto.Email);

                // Generate email confirmation token and send the activation email. This happens
                // *after* the commit above on purpose: the send must never be able to roll back an
                // account that was already created successfully (see
                // candidate-account-activation-email.md, §11 - no rollback policy). A failed send
                // is logged server-side only; the candidate can always recover via
                // POST /api/auth/resend-confirmation.
                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var activationLink = BuildActivationLink(user.Email!, emailConfirmationToken);

                var emailSent = await _emailNotificationService.SendAccountActivationEmailAsync(user.Email!, activationLink);
                if (!emailSent)
                {
                    _logger.LogWarning(
                        "Account activation email could not be sent to {Email}; account was created successfully but the candidate did not receive an activation link and must use the resend-confirmation endpoint.",
                        user.Email);
                }

                var authResponseDto = _mapper.Map<AuthResponseDto>(user);

                return AuthResult.SuccessWithUser(authResponseDto,
                    "Inscription réussie. Veuillez vérifier votre email pour confirmer votre compte.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred during candidate registration for {Email}", registerDto.Email);
                return AuthResult.Failure("Une erreur est survenue lors de l'inscription");
            }
        });
    }

    public async Task<AuthResult> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var validationResult = await _loginValidator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(errors);
            }

            // Check if we should redirect to Entra ID
            if (ShouldUseEntraId)
            {
                // Check if this email belongs to an organizational user (B2B)
                var potentialUser = await _userManager.FindByEmailAsync(loginDto.Email);
                if (potentialUser != null && potentialUser.OrganizationId.HasValue)
                {
                    // This is an organizational user - they should use Entra ID B2B authentication
                    var entraIdLoginUrl = GenerateEntraIdLoginUrl(loginDto.ReturnUrl ?? "/dashboard");
                    if (!string.IsNullOrEmpty(entraIdLoginUrl))
                    {
                        _logger.LogInformation("Redirecting organizational user {Email} to Entra ID B2B login",
                            loginDto.Email);
                        return AuthResult.Success("Veuillez utiliser votre compte organisationnel pour vous connecter", entraIdLoginUrl);
                    }
                }

                // For users without organization (candidates), they can still use JWT auth in staging/prod
                // or redirect to B2C if they prefer SSO
            }

            var user = await _userManager.Users
                .Include(u => u.Organization)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Address)
                .Include(u => u.Experiences)
                .Include(u => u.Trainings)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                return AuthResult.Failure("Identifiants invalides");
            }

            // Check if account is locked and auto-unlock if time has passed
            if (user.AccountLockedUntil.HasValue && user.AccountLockedUntil <= DateTime.UtcNow)
            {
                user.ResetFailedLogins();
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Account automatically unlocked for user: {Email}", loginDto.Email);
            }

            if (user.IsAccountLocked)
            {
                if (user.AccountLockedUntil.HasValue && user.AccountLockedUntil <= DateTime.UtcNow)
                {
                    user.ResetFailedLogins();
                }

                _logger.LogWarning("Login attempt on locked account: {Email}", loginDto.Email);
                return AuthResult.Failure($"Le compte est verrouillé jusqu'au {user.AccountLockedUntil:yyyy-MM-dd HH:mm}");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt on inactive account: {Email}", loginDto.Email);
                return AuthResult.Failure("Le compte est inactif");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Reset failed login attempts and update last login
                user.UpdateLastLogin();
                await _userManager.UpdateAsync(user);

                // Generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken();

                // Save refresh token
                user.SetRefreshToken(refreshToken, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
                await _userManager.UpdateAsync(user);

                var authResponseDto = _mapper.Map<AuthResponseDto>(user);
                authResponseDto.AccessToken = accessToken;

                _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);

                return AuthResult.SuccessWithUser(
                    authResponseDto,
                    "Connexion réussie"
                );
            }

            if (result.IsLockedOut)
            {
                user.IncrementFailedLogin();
                await _userManager.UpdateAsync(user);

                _logger.LogWarning("Account locked out for user: {Email}", loginDto.Email);
                return AuthResult.Failure("Compte verrouillé suite à plusieurs tentatives de connexion échouées");
            }

            if (result.IsNotAllowed)
            {
                _logger.LogWarning("Login not allowed for user: {Email}", loginDto.Email);
                var authResponseDto = _mapper.Map<AuthResponseDto>(user);
                return AuthResult.Failure(
                    "Votre compte n'est pas encore activé. Veuillez consulter l'email de confirmation envoyé lors de votre inscription, ou demandez un nouvel envoi.",
                    data: authResponseDto);
            }

            // Increment failed login attempts
            user.IncrementFailedLogin();
            await _userManager.UpdateAsync(user);

            _logger.LogWarning("Failed login attempt for user: {Email}", loginDto.Email);
            return AuthResult.Failure("Identifiants invalides");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for {Email}", loginDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors de la connexion");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var validationResult = await _refreshTokenValidator.ValidateAsync(refreshTokenDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(errors);
            }

            var user = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Address)
                .Include(u => u.Experiences)
                .Include(u => u.Trainings)
                .FirstOrDefaultAsync(u => u.Email == refreshTokenDto.Email);

            if (user == null || !user.IsTokenValid || user.RefreshToken != refreshTokenDto.RefreshToken)
            {
                _logger.LogWarning("Invalid refresh token attempt for {Email}", refreshTokenDto.Email);
                return AuthResult.Failure("Jeton de rafraîchissement invalide");
            }

            // Generate new tokens
            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Update refresh token
            user.SetRefreshToken(newRefreshToken, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Tokens refreshed for user: {Email}", refreshTokenDto.Email);
            var authResponseDto = _mapper.Map<AuthResponseDto>(user);
            authResponseDto.AccessToken = accessToken;
            return AuthResult.SuccessWithUser(
                authResponseDto,
                "Jeton rafraîchi avec succès"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token refresh for {Email}", refreshTokenDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors du rafraîchissement du jeton");
        }
    }

    public async Task<AuthResult> LogoutAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return AuthResult.Failure("Utilisateur introuvable");
            }

            // Clear refresh token
            user.ClearRefreshToken();
            await _userManager.UpdateAsync(user);

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {Email} logged out successfully", email);
            return AuthResult.Success("Déconnexion réussie");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout for {Email}", email);
            return AuthResult.Failure("Une erreur est survenue lors de la déconnexion");
        }
    }

    public async Task<AuthResult> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto)
    {
        try
        {
            var validationResult = await _confirmEmailValidator.ValidateAsync(confirmEmailDto);
            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(validationErrors);
            }

            var user = await _userManager.FindByEmailAsync(confirmEmailDto.Email);
            if (user == null)
            {
                return AuthResult.Failure("Utilisateur introuvable");
            }

            var result = await _userManager.ConfirmEmailAsync(user, confirmEmailDto.Token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for user: {Email}", confirmEmailDto.Email);
                return AuthResult.Success("Email confirmé avec succès");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AuthResult.Failure($"Échec de la confirmation de l'email : {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during email confirmation for {Email}", confirmEmailDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors de la confirmation de l'email");
        }
    }

    public async Task<AuthResult> ResendConfirmationEmailAsync(ResendConfirmationDto dto)
    {
        try
        {
            var validationResult = await _resendConfirmationValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(errors);
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || user.EmailConfirmed)
            {
                // Ne jamais révéler si le compte existe ou est déjà confirmé (enumeration-safety,
                // même principe que ForgotPasswordAsync ci-dessous).
                return AuthResult.Success(ResendConfirmationGenericMessage);
            }

            // Cooldown applicatif par email : protection additionnelle contre le bombardement
            // d'une boîte mail via plusieurs IP différentes, en complément du rate limiting par IP
            // au niveau du endpoint. Le résultat du cooldown n'est jamais révélé au client - même
            // message générique dans tous les cas.
            if (!ResendConfirmationCooldown.TryStart(user.Email!, TimeSpan.FromSeconds(60)))
            {
                return AuthResult.Success(ResendConfirmationGenericMessage);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var activationLink = BuildActivationLink(user.Email!, token);
            var sent = await _emailNotificationService.SendAccountActivationEmailAsync(user.Email!, activationLink);
            if (!sent)
            {
                _logger.LogError("Failed to resend account activation email to {Email}", user.Email);
            }

            return AuthResult.Success(ResendConfirmationGenericMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while resending confirmation email for {Email}", dto.Email);
            // Même message générique en cas d'exception inattendue - ne jamais révéler
            // d'information par une différence de message (enumeration-safety).
            return AuthResult.Success(ResendConfirmationGenericMessage);
        }
    }

    /// <summary>
    /// Construit le lien absolu d'activation de compte vers candidate-app, partagé entre
    /// <see cref="RegisterCandidateAsync"/> et <see cref="ResendConfirmationEmailAsync"/>.
    /// </summary>
    private string BuildActivationLink(string email, string token)
    {
        return $"{_frontendSettings.CandidateAppBaseUrl.TrimEnd('/')}/confirm-email" +
               $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            var validationResult = await _forgotPasswordValidator.ValidateAsync(forgotPasswordDto);
            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(validationErrors);
            }

            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null)
            {
                return AuthResult.Success("Si un compte existe avec cet email, un lien de réinitialisation a été envoyé");
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            user.SetPasswordResetToken(resetToken, TimeSpan.FromHours(1));
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password reset token generated for user: {Email}", forgotPasswordDto.Email);
            var authResponseDto = _mapper.Map<AuthResponseDto>(user);
            return AuthResult.SuccessWithUser(authResponseDto, "Email de réinitialisation du mot de passe envoyé");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset request for {Email}", forgotPasswordDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors du traitement de votre demande");
        }
    }

    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(resetPasswordDto);
            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(validationErrors);
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return AuthResult.Failure("Requête invalide");
            }

            var result =
                await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (result.Succeeded)
            {
                user.ClearPasswordResetToken();
                user.LastPasswordChangeAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Password reset successfully for user: {Email}", resetPasswordDto.Email);
                return AuthResult.Success("Réinitialisation du mot de passe réussie");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AuthResult.Failure($"Échec de la réinitialisation du mot de passe : {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset for {Email}", resetPasswordDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors de la réinitialisation du mot de passe");
        }
    }

    public async Task<AuthResult> AdminResetPasswordAsync(AdminResetPasswordDto adminResetPasswordDto)
    {
        try
        {
            var validationResult = await _adminResetPasswordValidator.ValidateAsync(adminResetPasswordDto);
            if (!validationResult.IsValid)
            {
                var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return AuthResult.ValidationError(validationErrors);
            }

            var targetUser = await _userManager.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == adminResetPasswordDto.Email);

            if (targetUser == null)
            {
                return AuthResult.Failure("Utilisateur introuvable");
            }

            // Get current admin user information
            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser == null)
            {
                return AuthResult.Failure("Contexte administrateur invalide");
            }

            // Check authorization - Organization admins can only reset passwords for users in their organization
            if (currentUser.IsInRole(Roles.OrganizationAdmin.Name) &&
                !currentUser.IsInRole(Roles.PlatformSuperAdmin.Name) &&
                !currentUser.IsInRole(Roles.PlatformAdmin.Name))
            {
                var adminOrgIdClaim = currentUser.FindFirst("OrganizationId");
                if (adminOrgIdClaim == null || !Guid.TryParse(adminOrgIdClaim.Value, out var adminOrgId))
                {
                    return AuthResult.Failure("Contexte d'organisation administrateur invalide");
                }

                if (targetUser.OrganizationId != adminOrgId)
                {
                    return AuthResult.Failure("Vous ne pouvez réinitialiser le mot de passe que des utilisateurs de votre organisation");
                }
            }

            // Remove current password and set new one
            var removePasswordResult = await _userManager.RemovePasswordAsync(targetUser);
            if (!removePasswordResult.Succeeded)
            {
                var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
                return AuthResult.Failure($"Échec de la suppression du mot de passe actuel : {errors}");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(targetUser, adminResetPasswordDto.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                return AuthResult.Failure($"Échec de la définition du nouveau mot de passe : {errors}");
            }

            // Update password change timestamp
            targetUser.LastPasswordChangeAt = DateTime.UtcNow;

            // Clear any existing refresh tokens to force re-authentication
            targetUser.ClearRefreshToken();

            await _userManager.UpdateAsync(targetUser);

            var adminEmail = currentUser.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
            _logger.LogInformation("Password reset by admin {AdminEmail} for user: {Email}",
                adminEmail, adminResetPasswordDto.Email);

            return AuthResult.Success($"Mot de passe réinitialisé avec succès pour {targetUser.Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during admin password reset for {Email}", adminResetPasswordDto.Email);
            return AuthResult.Failure("Une erreur est survenue lors de la réinitialisation du mot de passe");
        }
    }

    public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.Users
                .Include(u => u.Organization)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.Address)
                .Include(u => u.Experiences)
                .Include(u => u.Trainings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserDto>.NotFound("Utilisateur introuvable");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user {UserId}", userId);
            return ServiceResult<UserDto>.InternalError("Une erreur est survenue lors de la récupération des informations utilisateur");
        }
    }


    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // Add organization claim if user belongs to one
        if (user.OrganizationId.HasValue)
        {
            claims.Add(new Claim("OrganizationId", user.OrganizationId.Value.ToString()));
        }

        // Add role claims
        var userRoles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name).ToList();
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));


        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    #region Entra ID Authentication Methods

    public string GenerateEntraIdLoginUrl(string returnUrl = "/")
    {
        if (!ShouldUseEntraId)
        {
            _logger.LogWarning("Entra ID authentication is disabled. Use JWT authentication instead.");
            return string.Empty;
        }

        var state = GenerateStateParameter(returnUrl, "B2B");
        var nonce = Guid.NewGuid().ToString();

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _entraIdSettings.B2B.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _entraIdSettings.B2B.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(" ", _entraIdSettings.B2B.Scopes),
            ["state"] = state,
            ["nonce"] = nonce,
            ["prompt"] = "select_account"
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

        var authorizeUrl = $"{_entraIdSettings.B2B.Authority}/oauth2/v2.0/authorize?{queryString}";

        _logger.LogInformation("Generated B2B login URL for client_id: {ClientId}", _entraIdSettings.B2B.ClientId);
        return authorizeUrl;
    }

    public string GenerateEntraIdSignUpUrl(string returnUrl = "/profile")
    {
        if (!ShouldUseEntraId)
        {
            _logger.LogWarning("Entra ID authentication is disabled. Use JWT authentication instead.");
            return string.Empty;
        }

        var state = GenerateStateParameter(returnUrl, "B2C");
        var nonce = Guid.NewGuid().ToString();

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _entraIdSettings.B2C.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _entraIdSettings.B2C.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(" ", _entraIdSettings.B2C.Scopes),
            ["state"] = state,
            ["nonce"] = nonce,
            ["p"] = _entraIdSettings.B2C.SignUpSignInPolicyId
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

        var authorizeUrl = $"{_entraIdSettings.B2C.Authority}/oauth2/v2.0/authorize?{queryString}";

        _logger.LogInformation("Generated B2C signup URL for client_id: {ClientId}", _entraIdSettings.B2C.ClientId);
        return authorizeUrl;
    }

    public async Task<AuthResult> HandleEntraIdCallback(string code, string state, string? error = null)
    {
        try
        {
            if (!ShouldUseEntraId)
            {
                return AuthResult.Failure("L'authentification Entra ID est désactivée");
            }

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Entra ID callback returned error: {Error}", error);
                return AuthResult.Failure($"Échec de l'authentification : {error}");
            }

            if (string.IsNullOrEmpty(code))
            {
                return AuthResult.Failure("Le code d'autorisation est obligatoire");
            }

            // Parse and validate state parameter
            var returnUrl = ParseStateParameter(state);

            // Exchange authorization code for tokens
            var tokenResult = await ExchangeAuthCodeForTokens(code, state);
            if (!tokenResult.IsSuccess)
            {
                return tokenResult;
            }

            var tokenResponse = tokenResult.Data as dynamic;
            var accessToken = tokenResponse?.access_token?.ToString();
            var idToken = tokenResponse?.id_token?.ToString();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(idToken))
            {
                return AuthResult.Failure("Échec de l'obtention des jetons depuis Entra ID");
            }

            // Extract user information from ID token
            var userInfo = GetUserFromEntraIdToken(idToken);
            if (userInfo == null)
            {
                return AuthResult.Failure("Échec de l'extraction des informations utilisateur depuis le jeton");
            }

            // Find or create user in database (this will be handled by ClaimsEnrichmentMiddleware)
            string userEmail = userInfo.Email;
            var existingUser = await _userManager.FindByEmailAsync(userEmail);

            if (existingUser != null)
            {
                // Update last login
                existingUser.UpdateLastLogin();
                await _userManager.UpdateAsync(existingUser);

                // Generate JWT tokens for API usage
                var jwtAccessToken = GenerateAccessToken(existingUser);
                var refreshToken = GenerateRefreshToken();

                existingUser.SetRefreshToken(refreshToken, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));
                await _userManager.UpdateAsync(existingUser);

                _logger.LogInformation("Entra ID user {Email} authenticated successfully", userEmail);
                var authResponseDto = _mapper.Map<AuthResponseDto>(existingUser);
                authResponseDto.AccessToken = jwtAccessToken;
                authResponseDto.RedirectUrl = returnUrl;
                return AuthResult.SuccessWithUser(
                    authResponseDto,
                    "Authentification Entra ID réussie"
                );
            }

            // New user will be created by ClaimsEnrichmentMiddleware
            _logger.LogInformation("New Entra ID user {Email} will be created via middleware", userEmail);

            return AuthResult.Success("Authentification réussie. Le profil utilisateur sera créé automatiquement.",
                returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Entra ID callback processing");
            return AuthResult.Failure("Une erreur est survenue lors de l'authentification");
        }
    }

    public async Task<AuthResult> LinkEntraIdAccount(string userId, string entraIdToken)
    {
        try
        {
            if (!ShouldUseEntraId)
            {
                return AuthResult.Failure("La liaison Entra ID est désactivée");
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return AuthResult.Failure("Identifiant utilisateur invalide");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return AuthResult.Failure("Utilisateur introuvable");
            }

            // Extract user info from Entra ID token
            var entraUserInfo = GetUserFromEntraIdToken(entraIdToken);
            if (entraUserInfo == null)
            {
                return AuthResult.Failure("Jeton Entra ID invalide");
            }

            // Check if email matches
            if (!string.Equals(user.Email, entraUserInfo.Email, StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Failure(
                    "Les adresses email ne correspondent pas. Impossible de lier des comptes avec des adresses email différentes.");
            }

            // Check if this Entra ID is already linked to another user
            var existingUserWithEntraId = await _userManager.Users
                .FirstOrDefaultAsync(u => u.ExternalId == entraUserInfo.ExternalId && u.Id != userGuid);

            if (existingUserWithEntraId != null)
            {
                return AuthResult.Failure("Ce compte Entra ID est déjà lié à un autre utilisateur");
            }

            // Link accounts
            user.ExternalId = entraUserInfo.ExternalId;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully linked Entra ID account {EntraId} to user {UserId}",
                    entraUserInfo.ExternalId, userId);
                return AuthResult.Success("Compte lié avec succès à Entra ID");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AuthResult.Failure($"Échec de la liaison du compte : {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while linking Entra ID account for user {UserId}", userId);
            return AuthResult.Failure("Une erreur est survenue lors de la liaison des comptes");
        }
    }

    public EntraIdUserInfo? GetUserFromEntraIdToken(string idToken)
    {
        try
        {
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(idToken);

            var userInfo = new EntraIdUserInfo
            {
                ExternalId = token.Claims.FirstOrDefault(c => c.Type is "sub" or "oid")?.Value ?? string.Empty,
                Email = token.Claims.FirstOrDefault(c => c.Type is "email" or ClaimTypes.Email)?.Value ?? string.Empty,
                FirstName = token.Claims.FirstOrDefault(c => c.Type is "given_name" or ClaimTypes.GivenName)?.Value ??
                            string.Empty,
                LastName = token.Claims.FirstOrDefault(c => c.Type is "family_name" or ClaimTypes.Surname)?.Value ??
                           string.Empty,
                DisplayName = token.Claims.FirstOrDefault(c => c.Type is "name" or ClaimTypes.Name)?.Value ??
                              string.Empty,
                Groups = token.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList(),
                Roles = token.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList(),
                AuthType = DetermineAuthType(token.Claims)
            };

            // If display name is empty, construct it from first and last name
            if (string.IsNullOrEmpty(userInfo.DisplayName) && (!string.IsNullOrEmpty(userInfo.FirstName) ||
                                                               !string.IsNullOrEmpty(userInfo.LastName)))
            {
                userInfo.DisplayName = $"{userInfo.FirstName} {userInfo.LastName}".Trim();
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while parsing Entra ID token");
            return null;
        }
    }

    private static string DetermineAuthType(IEnumerable<Claim> claims)
    {
        var issuer = claims.FirstOrDefault(c => c.Type == "iss")?.Value ?? string.Empty;

        // B2C issuers typically contain the tenant name and policy
        if (issuer.Contains(".b2clogin.com") || issuer.Contains("tfp"))
        {
            return "B2C";
        }

        // B2B issuers typically use login.microsoftonline.com
        if (issuer.Contains("login.microsoftonline.com"))
        {
            return "B2B";
        }

        return "Unknown";
    }

    private async Task<AuthResult> ExchangeAuthCodeForTokens(string code, string state)
    {
        try
        {
            var authType = DetermineAuthTypeFromState(state);
            var tokenEndpoint = authType == "B2C"
                ? $"{_entraIdSettings.B2C.Authority}/oauth2/v2.0/token?p={_entraIdSettings.B2C.SignUpSignInPolicyId}"
                : $"{_entraIdSettings.B2B.Authority}/oauth2/v2.0/token";

            var clientId = authType == "B2C" ? _entraIdSettings.B2C.ClientId : _entraIdSettings.B2B.ClientId;
            var clientSecret =
                authType == "B2C" ? _entraIdSettings.B2C.ClientSecret : _entraIdSettings.B2B.ClientSecret;
            var redirectUri = authType == "B2C" ? _entraIdSettings.B2C.RedirectUri : _entraIdSettings.B2B.RedirectUri;
            var scopes = authType == "B2C" ? _entraIdSettings.B2C.Scopes : _entraIdSettings.B2B.Scopes;

            using var httpClient = new HttpClient();

            var tokenRequestParams = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["scope"] = string.Join(" ", scopes)
            };

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(tokenRequestParams)
            };

            var response = await httpClient.SendAsync(tokenRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {StatusCode} - {Content}", response.StatusCode,
                    responseContent);
                return AuthResult.Failure("Échec de l'échange du code d'autorisation contre des jetons");
            }

            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<object>(responseContent);
            return AuthResult.SuccessWithData(tokenResponse!, "Échange de jeton réussi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token exchange");
            return AuthResult.Failure("Échec de l'échange de jeton");
        }
    }

    private static string DetermineAuthTypeFromState(string state)
    {
        try
        {
            var stateJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var stateData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(stateJson);

            if (stateData?.TryGetValue("authType", out var authTypeValue) == true)
            {
                return authTypeValue?.ToString() ?? "B2B";
            }
        }
        catch
        {
            // If we can't parse state, assume B2B 
        }

        return "B2B";
    }

    private static string ParseStateParameter(string state)
    {
        try
        {
            var stateJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var stateData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(stateJson);

            if (stateData?.TryGetValue("returnUrl", out var returnUrlValue) == true)
            {
                return returnUrlValue?.ToString() ?? "/";
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - just return default
            Console.WriteLine($"Failed to parse state parameter: {ex.Message}");
        }

        return "/";
    }

    private static string GenerateStateParameter(string returnUrl, string authType = "B2B")
    {
        var stateData = new
        {
            returnUrl,
            authType,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            nonce = Guid.NewGuid().ToString("N")[..8]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(stateData);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    }

    #endregion

    #region URL Generation Methods

    public ServiceResult<AuthUrlResponseDto> GetLoginUrl(string? email, string? returnUrl)
    {
        try
        {
            var loginUrl = GenerateEntraIdLoginUrl(returnUrl ?? "/dashboard");

            if (string.IsNullOrEmpty(loginUrl))
            {
                var response = new AuthUrlResponseDto
                {
                    UseLocalAuth = true,
                    LocalEndpoint = "/api/auth/login",
                    Message = "Utiliser l'authentification locale"
                };
                return ServiceResult<AuthUrlResponseDto>.Success(response);
            }

            var entraResponse = new AuthUrlResponseDto
            {
                UseLocalAuth = false,
                EntraIdUrl = loginUrl,
                AuthType = "B2B",
                Message = "Rediriger vers Entra ID pour l'authentification"
            };

            return ServiceResult<AuthUrlResponseDto>.Success(entraResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating login URL for email {Email}", email);
            return ServiceResult<AuthUrlResponseDto>.InternalError("Échec de la génération de l'URL de connexion");
        }
    }

    public ServiceResult<AuthUrlResponseDto> GetSignupUrl(string? returnUrl)
    {
        try
        {
            var signupUrl = GenerateEntraIdSignUpUrl(returnUrl ?? "/profile");

            if (string.IsNullOrEmpty(signupUrl))
            {
                var response = new AuthUrlResponseDto
                {
                    UseLocalAuth = true,
                    LocalEndpoint = "/api/auth/register",
                    Message = "Utiliser l'inscription locale"
                };
                return ServiceResult<AuthUrlResponseDto>.Success(response);
            }

            var entraResponse = new AuthUrlResponseDto
            {
                UseLocalAuth = false,
                EntraIdUrl = signupUrl,
                AuthType = "B2C",
                Message = "Rediriger vers Entra ID pour l'inscription"
            };

            return ServiceResult<AuthUrlResponseDto>.Success(entraResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signup URL");
            return ServiceResult<AuthUrlResponseDto>.InternalError("Échec de la génération de l'URL d'inscription");
        }
    }

    public async Task<ServiceResult<UserTypeResponseDto>> GetUserTypeAsync(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return ServiceResult<UserTypeResponseDto>.ValidationError(["L'adresse email est obligatoire"]);
            }

            // Check if user exists in database
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                var userType = existingUser.OrganizationId.HasValue ? "organizational" : "candidate";
                var authType = existingUser.OrganizationId.HasValue ? "B2B" : "B2C";
                var endpoint = existingUser.OrganizationId.HasValue ? "/api/auth/login-url" : "/api/auth/signup-url";

                var response = new UserTypeResponseDto
                {
                    Email = email,
                    UserType = userType,
                    RecommendedAuth = authType,
                    AuthEndpoint = endpoint,
                    Message = userType == "organizational"
                        ? "Utiliser le compte organisationnel pour l'authentification"
                        : "Utiliser l'authentification candidat"
                };

                return ServiceResult<UserTypeResponseDto>.Success(response);
            }

            // For new users, determine type based on email domain
            var domain = email.Split('@').LastOrDefault();
            var isOrganizationalDomain = !string.IsNullOrEmpty(domain) &&
                                         !new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com" }.Contains(
                                             domain.ToLower());

            var newUserResponse = new UserTypeResponseDto
            {
                Email = email,
                UserType = isOrganizationalDomain ? "organizational" : "candidate",
                RecommendedAuth = isOrganizationalDomain ? "B2B" : "B2C",
                AuthEndpoint = isOrganizationalDomain ? "/api/auth/login-url" : "/api/auth/signup-url",
                Message = isOrganizationalDomain
                    ? "Utiliser le compte organisationnel pour l'authentification"
                    : "Utiliser l'inscription ou l'authentification candidat"
            };

            return ServiceResult<UserTypeResponseDto>.Success(newUserResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining user type for email {Email}", email);
            return ServiceResult<UserTypeResponseDto>.InternalError("Échec de la détermination du type d'utilisateur");
        }
    }

    #endregion
}