using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Services;

public class UserService : IUserService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserDto> _createUserValidator;
    private readonly IValidator<UpdateUserDto> _updateUserValidator;
    private readonly IValidator<UserFilterDto> _filterValidator;
    private readonly IValidator<UploadCvDto> _uploadCvValidator;
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IResumeService _resumeService;

    public UserService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<CreateUserDto> createUserValidator,
        IValidator<UpdateUserDto> updateUserValidator,
        IValidator<UserFilterDto> filterValidator,
        IValidator<UploadCvDto> uploadCvValidator,
        ILogger<UserService> logger,
        UserManager<User> userManager,
        ICurrentUserService currentUserService,
        IResumeService resumeService)
    {
        _context = context;
        _mapper = mapper;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
        _filterValidator = filterValidator;
        _uploadCvValidator = uploadCvValidator;
        _logger = logger;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _resumeService = resumeService;
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Experiences)
                .Include(u => u.Trainings)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ServiceResult<UserDto>.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return ServiceResult<UserDto>.InternalError("Une erreur est survenue lors de la récupération de l'utilisateur");
        }
    }

    public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(Guid id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ServiceResult<UserProfileDto>.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            var userProfileDto = _mapper.Map<UserProfileDto>(user);
            return ServiceResult<UserProfileDto>.Success(userProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile with ID {UserId}", id);
            return ServiceResult<UserProfileDto>.InternalError("Une erreur est survenue lors de la récupération du profil utilisateur");
        }
    }

    public async Task<ServiceResult<List<UserSearchResultDto>>> GetAllAsync()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserSearchResultDto>>(users);
            return ServiceResult<List<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return ServiceResult<List<UserSearchResultDto>>.InternalError("Une erreur est survenue lors de la récupération des utilisateurs");
        }
    }

    public async Task<PaginatedResult<UserSearchResultDto>> SearchAsync(UserFilterDto filter)
    {
        try
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return PaginatedResult<UserSearchResultDto>.Failure(errors, "Paramètres de filtre invalides");
            }

            var query = BuildUserQuery(filter);

            var pageNumber = int.TryParse(filter.PageNumber, out var pn) ? pn : 1;
            var pageSize = int.TryParse(filter.PageSize, out var ps) ? ps : 10;

            var paginatedResult = await query.ToPaginatedResultAsync(pageNumber, pageSize);

            return paginatedResult.Map(user => _mapper.Map<UserSearchResultDto>(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with filter {Filter}", filter);
            return PaginatedResult<UserSearchResultDto>.Failure("Une erreur est survenue lors de la recherche des utilisateurs");
        }
    }

    public async Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto)
    {
        try
        {
            var validationResult = await _createUserValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<UserDto>.ValidationError(errors);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
            {
                return ServiceResult<UserDto>.Conflict($"Un utilisateur avec l'email '{dto.Email}' existe déjà");
            }

            // Validate organization exists for internal users
            if (dto.OrganizationId.HasValue)
            {
                var orgExists = await _context.Organizations.AnyAsync(o => o.Id == dto.OrganizationId.Value);
                if (!orgExists)
                {
                    return ServiceResult<UserDto>.Failure($"Organisation avec l'ID {dto.OrganizationId} introuvable");
                }
            }

            // Additional security check: OrganizationAdmin can only create users for their own organization
            if (_currentUserService.User?.IsInRole(Roles.OrganizationAdmin.Name) == true)
            {
                var currentUserOrgId = _currentUserService.OrganizationId;
                if (currentUserOrgId.HasValue && dto.OrganizationId != currentUserOrgId)
                {
                    return ServiceResult<UserDto>.Forbidden(
                        "Un administrateur d'organisation ne peut créer des utilisateurs que pour sa propre organisation");
                }
            }

            var user = _mapper.Map<User>(dto);
            user.Id = Guid.NewGuid();
            user.UserName = dto.Email; // UserManager requires UserName
            user.CalculateProfileCompletion();

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user {Email}: {Errors}", dto.Email, errors);
                return ServiceResult<UserDto>.Failure($"Échec de la création de l'utilisateur : {errors}");
            }

            _logger.LogInformation("Created new user with ID {UserId} and email {Email}", user.Id, user.Email);

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto, "Utilisateur créé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating user with email {DtoEmail}", dto.Email);
            return ServiceResult<UserDto>.InternalError("Une erreur est survenue lors de la création de l'utilisateur");
        }
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        try
        {
            var validationResult = await _updateUserValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<UserDto>.ValidationError(errors);
            }

            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ServiceResult<UserDto>.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            // Check email uniqueness if email is being changed
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists)
                {
                    return ServiceResult<UserDto>.Conflict($"Un utilisateur avec l'email '{dto.Email}' existe déjà");
                }
            }

            // Validate organization exists if being changed
            if (dto.OrganizationId.HasValue && dto.OrganizationId != user.OrganizationId)
            {
                var orgExists = await _context.Organizations.AnyAsync(o => o.Id == dto.OrganizationId.Value);
                if (!orgExists)
                {
                    return ServiceResult<UserDto>.Failure($"Organisation avec l'ID {dto.OrganizationId} introuvable");
                }
            }

            _mapper.Map(dto, user);
            user.UpdatedAt = DateTime.UtcNow;

            user.CalculateProfileCompletion();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user with ID {UserId}", id);

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto, "Utilisateur mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {Id}", id);
            return ServiceResult<UserDto>.InternalError("Une erreur est survenue lors de la mise à jour de l'utilisateur");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            // Softly delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted user with ID {UserId}", id);
            return ServiceResult.Success("Utilisateur supprimé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression de l'utilisateur");
        }
    }

    public async Task<ServiceResult> HardDeleteAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hard deleted user with ID {UserId}", id);
            return ServiceResult.Success("Utilisateur supprimé définitivement");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting user with ID {UserId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression définitive de l'utilisateur");
        }
    }

    public async Task<ServiceResult<UploadCvResponseDto>> UploadCvAsync(Guid userId, UploadCvDto dto)
    {
        try
        {
            // Validate input
            var validationResult = await _uploadCvValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<UploadCvResponseDto>.ValidationError(errors);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<UploadCvResponseDto>.NotFound($"Utilisateur avec l'ID {userId} introuvable");
            }

            // Delete existing CV from blob storage if replacing
            if (dto.ReplaceExisting && !string.IsNullOrEmpty(user.CvPath))
            {
                await _resumeService.DeleteResumeAsync(user.CvPath);
            }

            // Upload CV to Azure Blob Storage
            var uploadResult = await _resumeService.UploadResumeAsync(dto.CvFile, userId);
            if (!uploadResult.IsSuccess)
            {
                return ServiceResult<UploadCvResponseDto>.Failure($"Échec du téléversement du CV : {uploadResult.Message}");
            }

            // Update user CV path with the blob URL
            user.CvPath = uploadResult.Data;
            user.UpdatedAt = DateTime.UtcNow;
            user.CalculateProfileCompletion();

            await _context.SaveChangesAsync();

            var response = new UploadCvResponseDto
            {
                Success = true,
                Message = "CV téléversé avec succès",
                CvPath = uploadResult.Data, // This is now the Azure Blob Storage URL
                FileName = Path.GetFileName(new Uri(uploadResult.Data).LocalPath),
                FileSizeBytes = dto.CvFile.Length,
                UploadedAt = DateTime.UtcNow
            };

            // TODO: If ExtractInformation is true, call CV analysis service
            if (dto.ExtractInformation)
            {
                _logger.LogInformation("CV information extraction requested for user {UserId} but not implemented yet",
                    userId);
                // response.ExtractedInfo = await ExtractCvInformation(filePath);
            }

            _logger.LogInformation("Uploaded CV to Blob Storage for user {UserId}: {CvPath}", userId, uploadResult.Data);
            return ServiceResult<UploadCvResponseDto>.Success(response, "CV téléversé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CV for user {UserId}", userId);
            return ServiceResult<UploadCvResponseDto>.InternalError("Une erreur est survenue lors du téléversement du CV");
        }
    }

    public async Task<ServiceResult<IEnumerable<UserSearchResultDto>>> GetByOrganizationAsync(Guid organizationId)
    {
        try
        {
            var orgExists = await _context.Organizations.AnyAsync(o => o.Id == organizationId);
            if (!orgExists)
            {
                return ServiceResult<IEnumerable<UserSearchResultDto>>.NotFound(
                    $"Organisation avec l'ID {organizationId} introuvable");
            }

            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.OrganizationId == organizationId && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userDtos = users.Select(user => _mapper.Map<UserSearchResultDto>(user)).ToList();

            _logger.LogInformation("Retrieved {Count} users for organization {OrganizationId}", userDtos.Count,
                organizationId);
            return ServiceResult<IEnumerable<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for organization {OrganizationId}", organizationId);
            return ServiceResult<IEnumerable<UserSearchResultDto>>.InternalError(
                "Une erreur est survenue lors de la récupération des utilisateurs de l'organisation");
        }
    }

    public async Task<ServiceResult> ActivateAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            if (user.IsActive)
            {
                return ServiceResult.Success("L'utilisateur est déjà actif");
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Activated user with ID {UserId}", id);
            return ServiceResult.Success("Utilisateur activé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user with ID {UserId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de l'activation de l'utilisateur");
        }
    }

    public async Task<ServiceResult> DeactivateAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            if (!user.IsActive)
            {
                return ServiceResult.Success("L'utilisateur est déjà inactif");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated user with ID {UserId}", id);
            return ServiceResult.Success("Utilisateur désactivé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user with ID {UserId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la désactivation de l'utilisateur");
        }
    }

    public async Task<ServiceResult<bool>> ExistsAsync(Guid id)
    {
        try
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == id);
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists with ID {UserId}", id);
            return ServiceResult<bool>.InternalError("Une erreur est survenue lors de la vérification de l'existence de l'utilisateur");
        }
    }

    public async Task<ServiceResult<bool>> EmailExistsAsync(string email, Guid? excludeUserId = null)
    {
        try
        {
            var query = _context.Users.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            var exists = await query.AnyAsync();
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email exists: {Email}", email);
            return ServiceResult<bool>.InternalError("Une erreur est survenue lors de la vérification de l'existence de l'email");
        }
    }

    public async Task<ServiceResult> UpdateLastLoginAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated last login for user {UserId}", id);
            return ServiceResult.Success("Dernière connexion mise à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {UserId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la mise à jour de la dernière connexion");
        }
    }

    public async Task<ServiceResult<int>> UpdateProfileCompletionAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult<int>.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            user.CalculateProfileCompletion();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated profile completion for user {UserId}: {Completion}%", id,
                user.ProfileCompletionPercentage);
            return ServiceResult<int>.Success(user.ProfileCompletionPercentage,
                "Complétion du profil mise à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile completion for user {UserId}", id);
            return ServiceResult<int>.InternalError("Une erreur est survenue lors de la mise à jour de la complétion du profil");
        }
    }

    public async Task<ServiceResult<List<UserSearchResultDto>>> GetUsersWithIncompleteProfilesAsync(int threshold = 80)
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.IsActive && u.ProfileCompletionPercentage < threshold)
                .OrderBy(u => u.ProfileCompletionPercentage)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var userDtos = users.Select(user => _mapper.Map<UserSearchResultDto>(user)).ToList();

            _logger.LogInformation("Found {Count} users with incomplete profiles (threshold: {Threshold}%)",
                userDtos.Count, threshold);
            return ServiceResult<List<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with incomplete profiles");
            return ServiceResult<List<UserSearchResultDto>>.InternalError(
                "Une erreur est survenue lors de la récupération des utilisateurs avec profils incomplets");
        }
    }

    public async Task<ServiceResult<List<UserSearchResultDto>>> GetRecentlyRegisteredUsersAsync(int days = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.IsActive && u.CreatedAt >= cutoffDate)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(user => _mapper.Map<UserSearchResultDto>(user)).ToList();

            _logger.LogInformation("Found {Count} recently registered users (last {Days} days)", userDtos.Count, days);
            return ServiceResult<List<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recently registered users");
            return ServiceResult<List<UserSearchResultDto>>.InternalError(
                "Une erreur est survenue lors de la récupération des utilisateurs récemment inscrits");
        }
    }

    public async Task<ServiceResult<List<UserSearchResultDto>>> GetInactiveUsersAsync(int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.IsActive &&
                            (u.LastLoginAt == null || u.LastLoginAt < cutoffDate))
                .OrderBy(u => u.LastLoginAt ?? u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(user => _mapper.Map<UserSearchResultDto>(user)).ToList();

            _logger.LogInformation("Found {Count} inactive users (no login for {Days} days)", userDtos.Count, days);
            return ServiceResult<List<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inactive users");
            return ServiceResult<List<UserSearchResultDto>>.InternalError(
                "Une erreur est survenue lors de la récupération des utilisateurs inactifs");
        }
    }

    public async Task<ServiceResult<int>> BulkUpdateAsync(List<Guid> userIds, UpdateUserDto updates)
    {
        try
        {
            if (!userIds.Any())
            {
                return ServiceResult<int>.Success(0, "Aucun utilisateur à mettre à jour");
            }

            var validationResult = await _updateUserValidator.ValidateAsync(updates);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<int>.ValidationError(errors);
            }

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            if (!users.Any())
            {
                return ServiceResult<int>.NotFound("Aucun utilisateur trouvé avec les identifiants fournis");
            }

            int updatedCount = 0;
            foreach (var user in users)
            {
                try
                {
                    // Check email uniqueness if email is being changed
                    if (!string.IsNullOrEmpty(updates.Email) && updates.Email != user.Email)
                    {
                        var emailExists =
                            await _context.Users.AnyAsync(u => u.Email == updates.Email && u.Id != user.Id);
                        if (emailExists)
                        {
                            _logger.LogWarning("Skipping user {UserId} - email already exists: {Email}", user.Id,
                                updates.Email);
                            continue;
                        }
                    }

                    // Apply updates
                    _mapper.Map(updates, user);
                    user.UpdatedAt = DateTime.UtcNow;
                    user.CalculateProfileCompletion();

                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {UserId} during bulk update", user.Id);
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk updated {UpdatedCount} out of {RequestedCount} users", updatedCount,
                    userIds.Count);
            }

            return ServiceResult<int>.Success(updatedCount, $"{updatedCount} utilisateur(s) mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update of users");
            return ServiceResult<int>.InternalError("Une erreur est survenue lors de la mise à jour groupée");
        }
    }

    private IQueryable<User> BuildUserQuery(UserFilterDto filter)
    {
        var query = _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Check if current user has Organization.Admin or Manager role
        if (filter.Role != Roles.Candidate.Name && _currentUserService.User?.Identity?.IsAuthenticated == true)
        {
            var isOrgAdmin = _currentUserService.User.IsInRole(Roles.OrganizationAdmin.Name);
            var isManager = _currentUserService.User.IsInRole(Roles.Manager.Name);
            var isPlatformUser = _currentUserService.User.IsInRole(Roles.PlatformSuperAdmin.Name) ||
                                 _currentUserService.User.IsInRole(Roles.PlatformAdmin.Name);


            if ((isOrgAdmin || isManager) && !isPlatformUser && _currentUserService.OrganizationId.HasValue)
            {
                // Filter to only show users from their organization
                query = query.Where(u => u.OrganizationId == _currentUserService.OrganizationId.Value);
            }
        }

        // Apply filters
        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == filter.OrganizationId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Department))
        {
            query = query.Where(u => u.Department != null && u.Department.Contains(filter.Department));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        if (filter.MinExperience.HasValue)
        {
            query = query.Where(u => u.YearsOfExperience >= filter.MinExperience.Value);
        }

        if (filter.MaxExperience.HasValue)
        {
            query = query.Where(u => u.YearsOfExperience <= filter.MaxExperience.Value);
        }

        if (filter.MinSalary.HasValue)
        {
            query = query.Where(u => u.DesiredSalary >= filter.MinSalary.Value);
        }

        if (filter.MaxSalary.HasValue)
        {
            query = query.Where(u => u.DesiredSalary <= filter.MaxSalary.Value);
        }

        if (!string.IsNullOrEmpty(filter.Skills))
        {
            query = query.Where(u => u.Skills != null && u.Skills.Contains(filter.Skills));
        }

        if (!string.IsNullOrEmpty(filter.Role))
        {
            query = query.Where(u =>
                u.UserRoles.Any(ur => ur.IsActive && ur.Role.Name.ToLower() == filter.Role.ToLower()));
        }

        // Search terms
        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            var searchTerms = filter.SearchTerms.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(searchTerms) ||
                u.LastName.ToLower().Contains(searchTerms) ||
                u.Email!.ToLower().Contains(searchTerms) ||
                u.Address.City!.ToLower().Contains(searchTerms) ||
                u.Address.Country!.ToLower().Contains(searchTerms) ||
                u.Address.PostalCode!.ToLower().Contains(searchTerms) ||
                u.Address.Region!.ToLower().Contains(searchTerms) ||
                u.Address.StreetName!.ToLower().Contains(searchTerms) ||
                u.Address.AddressLine2!.ToLower().Contains(searchTerms) ||
                u.Address.StreetNumber!.ToLower().Contains(searchTerms) ||
                (u.Skills != null && u.Skills.ToLower().Contains(searchTerms)));
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);
        }
        else
        {
            query = filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt);
        }

        return query;
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string sortBy, SortDirection sortDirection)
    {
        return sortBy.ToLower() switch
        {
            "firstname" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.FirstName)
                : query.OrderByDescending(u => u.FirstName),
            "lastname" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.LastName)
                : query.OrderByDescending(u => u.LastName),
            "email" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.Email)
                : query.OrderByDescending(u => u.Email),
            "createdat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt),
            "lastloginat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.LastLoginAt)
                : query.OrderByDescending(u => u.LastLoginAt),
            "experience" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.YearsOfExperience)
                : query.OrderByDescending(u => u.YearsOfExperience),
            "desiredsalary" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.DesiredSalary)
                : query.OrderByDescending(u => u.DesiredSalary),

            _ => sortDirection == SortDirection.Ascending
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt)
        };
    }

    public async Task<ServiceResult<UserDto>> UpdateSkillsAsync(Guid id, UpdateUserSkillsDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult<UserDto>.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            // Replace skills
            user.Skills = dto.Skills;
            user.UpdatedAt = DateTime.UtcNow;

            // Recalculate profile completion
            user.CalculateProfileCompletion();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated skills for user {UserId}", id);

            // Get updated user with relations
            var updatedUser = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            var userDto = _mapper.Map<UserDto>(updatedUser);
            return ServiceResult<UserDto>.Success(userDto, "Compétences mises à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skills for user {UserId}", id);
            return ServiceResult<UserDto>.InternalError("Une erreur est survenue lors de la mise à jour des compétences de l'utilisateur");
        }
    }

    public async Task<ServiceResult<UserDto>> UpdateProfileAsync(Guid id, UpdateUserProfileDto dto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return ServiceResult<UserDto>.NotFound($"Utilisateur avec l'ID {id} introuvable");
            }

            // Update user properties
            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
                user.PhoneNumber = dto.PhoneNumber;

            if (dto.YearsOfExperience.HasValue)
                user.YearsOfExperience = dto.YearsOfExperience;

            if (dto.DesiredSalary.HasValue)
                user.DesiredSalary = dto.DesiredSalary;

            if (dto.DesiredSalaryCurrency.HasValue)
                user.DesiredSalaryCurrency = dto.DesiredSalaryCurrency;

            if (dto.Availability.HasValue)
                user.Availability = dto.Availability;

            if (!string.IsNullOrEmpty(dto.LinkedInProfile))
                user.LinkedInProfile = dto.LinkedInProfile;

            // Update address if provided
            if (HasAddressData(dto))
            {
                if (!string.IsNullOrEmpty(dto.StreetNumber))
                    user.Address.StreetNumber = dto.StreetNumber;

                if (!string.IsNullOrEmpty(dto.Street))
                    user.Address.StreetName = dto.Street;

                if (!string.IsNullOrEmpty(dto.City))
                    user.Address.City = dto.City;

                if (!string.IsNullOrEmpty(dto.PostalCode))
                    user.Address.PostalCode = dto.PostalCode;

                if (!string.IsNullOrEmpty(dto.Region))
                    user.Address.Region = dto.Region;

                if (!string.IsNullOrEmpty(dto.Country))
                    user.Address.Country = dto.Country;

                if (!string.IsNullOrEmpty(dto.AddressLine2))
                    user.Address.AddressLine2 = dto.AddressLine2;
            }

            user.UpdatedAt = DateTime.UtcNow;

            // Recalculate profile completion
            user.CalculateProfileCompletion();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated profile for user {UserId}", id);

            // Get updated user with all relations
            var updatedUser = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Address)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            var userDto = _mapper.Map<UserDto>(updatedUser);
            return ServiceResult<UserDto>.Success(userDto, "Profil mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", id);
            return ServiceResult<UserDto>.InternalError("Une erreur est survenue lors de la mise à jour du profil utilisateur");
        }
    }

    public async Task<ServiceResult<CvDownloadResult>> GetCvForDownloadAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResult<CvDownloadResult>.NotFound($"Utilisateur avec l'ID {userId} introuvable");
            }

            if (string.IsNullOrEmpty(user.CvPath))
            {
                return ServiceResult<CvDownloadResult>.NotFound("Aucun CV téléversé pour cet utilisateur");
            }

            var metadataResult = await _resumeService.GetResumeMetadataAsync(user.CvPath);
            if (!metadataResult.IsSuccess)
            {
                return ServiceResult<CvDownloadResult>.NotFound("Fichier CV introuvable dans le stockage");
            }

            var downloadResult = await _resumeService.DownloadResumeAsync(user.CvPath);
            if (!downloadResult.IsSuccess)
            {
                return ServiceResult<CvDownloadResult>.NotFound("Fichier CV introuvable dans le stockage");
            }

            var cvDownloadResult = new CvDownloadResult
            {
                Content = downloadResult.Data!,
                FileName = metadataResult.Data!.FileName,
                ContentType = metadataResult.Data!.ContentType
            };

            return ServiceResult<CvDownloadResult>.Success(cvDownloadResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CV for download for user {UserId}", userId);
            return ServiceResult<CvDownloadResult>.InternalError("Une erreur est survenue lors de la récupération du CV");
        }
    }

    private static bool HasAddressData(UpdateUserProfileDto dto)
    {
        return !string.IsNullOrEmpty(dto.StreetNumber) ||
               !string.IsNullOrEmpty(dto.Street) ||
               !string.IsNullOrEmpty(dto.City) ||
               !string.IsNullOrEmpty(dto.PostalCode) ||
               !string.IsNullOrEmpty(dto.Region) ||
               !string.IsNullOrEmpty(dto.Country) ||
               !string.IsNullOrEmpty(dto.AddressLine2);
    }
}