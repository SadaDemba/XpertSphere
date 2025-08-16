using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

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
    
    public UserService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<CreateUserDto> createUserValidator,
        IValidator<UpdateUserDto> updateUserValidator,
        IValidator<UserFilterDto> filterValidator,
        IValidator<UploadCvDto> uploadCvValidator,
        ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
        _filterValidator = filterValidator;
        _uploadCvValidator = uploadCvValidator;
        _logger = logger;
    }
    
    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
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
                return ServiceResult<UserDto>.NotFound($"User with ID {id} not found");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return ServiceResult<UserDto>.InternalError("An error occurred while retrieving the user");
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
                return ServiceResult<UserProfileDto>.NotFound($"User with ID {id} not found");
            }
            var userProfileDto = _mapper.Map<UserProfileDto>(user);
            return ServiceResult<UserProfileDto>.Success(userProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile with ID {UserId}", id);
            return ServiceResult<UserProfileDto>.InternalError("An error occurred while retrieving the user profile");
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
                return PaginatedResult<UserSearchResultDto>.Failure(errors, "Invalid filter parameters");
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
            return PaginatedResult<UserSearchResultDto>.Failure("An error occurred while searching users");
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
                return ServiceResult<UserDto>.Conflict($"User with email '{dto.Email}' already exists");
            }
            
            // Validate organization exists for internal users
            if (dto.OrganizationId.HasValue)
            {
                var orgExists = await _context.Organizations.AnyAsync(o => o.Id == dto.OrganizationId.Value);
                if (!orgExists)
                {
                    return ServiceResult<UserDto>.Failure($"Organization with ID {dto.OrganizationId} not found");
                }
            }
            
            var user = _mapper.Map<User>(dto);
            user.Id = Guid.NewGuid();
            user.CalculateProfileCompletion();
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new user with ID {UserId} and email {Email}", user.Id, user.Email);
            
            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating user with email {DtoEmail}", dto.Email);
            return ServiceResult<UserDto>.InternalError("An error occurred while creating the user");
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
                return ServiceResult<UserDto>.NotFound($"User with ID {id} not found");
            }

            // Check email uniqueness if email is being changed
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
                if (emailExists)
                {
                    return ServiceResult<UserDto>.Conflict($"User with email '{dto.Email}' already exists");
                }
            }

            // Validate organization exists if being changed
            if (dto.OrganizationId.HasValue && dto.OrganizationId != user.OrganizationId)
            {
                var orgExists = await _context.Organizations.AnyAsync(o => o.Id == dto.OrganizationId.Value);
                if (!orgExists)
                {
                    return ServiceResult<UserDto>.Failure($"Organization with ID {dto.OrganizationId} not found");
                }
            }
            
            _mapper.Map(dto, user);
            user.UpdatedAt = DateTime.UtcNow;
            
            user.CalculateProfileCompletion();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user with ID {UserId}", id);

            var userDto = _mapper.Map<UserDto>(user);
            return ServiceResult<UserDto>.Success(userDto, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {Id}", id);
            return ServiceResult<UserDto>.InternalError("An error occurred while updating the user");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"User with ID {id} not found");
            }

            // Softly delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted user with ID {UserId}", id);
            return ServiceResult.Success("User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the user");
        }
    }

    public async Task<ServiceResult> HardDeleteAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"User with ID {id} not found");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Hard deleted user with ID {UserId}", id);
            return ServiceResult.Success("User permanently deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting user with ID {UserId}", id);
            return ServiceResult.InternalError("An error occurred while permanently deleting the use");
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
                return ServiceResult<UploadCvResponseDto>.NotFound($"User with ID {userId} not found");
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(dto.CvFile.FileName);
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine("uploads", "cvs", fileName);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save file
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.CvFile.CopyToAsync(stream);
            }

            // Update user CV path
            if (dto.ReplaceExisting && !string.IsNullOrEmpty(user.CvPath))
            {
                // Delete an old CV file if it exists
                if (File.Exists(user.CvPath))
                {
                    File.Delete(user.CvPath);
                }
            }

            user.CvPath = filePath;
            user.UpdatedAt = DateTime.UtcNow;
            user.CalculateProfileCompletion();

            await _context.SaveChangesAsync();

            var response = new UploadCvResponseDto
            {
                Success = true,
                Message = "CV uploaded successfully",
                CvPath = filePath,
                FileName = fileName,
                FileSizeBytes = dto.CvFile.Length,
                UploadedAt = DateTime.UtcNow
            };

            // TODO: If ExtractInformation is true, call CV analysis service
            if (dto.ExtractInformation)
            {
                _logger.LogInformation("CV information extraction requested for user {UserId} but not implemented yet", userId);
                // response.ExtractedInfo = await ExtractCvInformation(filePath);
            }

            _logger.LogInformation("Uploaded CV for user {UserId}: {FileName}", userId, fileName);
            return ServiceResult<UploadCvResponseDto>.Success(response, "CV uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CV for user {UserId}", userId);
            return ServiceResult<UploadCvResponseDto>.InternalError("An error occurred while uploading the CV");
        }
    }

    public async Task<ServiceResult<List<UserSearchResultDto>>> GetByOrganizationAsync(Guid organizationId)
    {
        try
        {
            var orgExists = await _context.Organizations.AnyAsync(o => o.Id == organizationId);
            if (!orgExists)
            {
                return ServiceResult<List<UserSearchResultDto>>.NotFound($"Organization with ID {organizationId} not found");
            }

            var users = await _context.Users
                .Include(u => u.Organization)
                .Where(u => u.OrganizationId == organizationId && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userDtos = users.Select(user => _mapper.Map<UserSearchResultDto>(user)).ToList();
            
            _logger.LogInformation("Retrieved {Count} users for organization {OrganizationId}", userDtos.Count, organizationId);
            return ServiceResult<List<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for organization {OrganizationId}", organizationId);
            return ServiceResult<List<UserSearchResultDto>>.InternalError("An error occurred while retrieving users for the organization");
        }
    }

    public async Task<ServiceResult> ActivateAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"User with ID {id} not found");
            }

            if (user.IsActive)
            {
                return ServiceResult.Success("User is already active");
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Activated user with ID {UserId}", id);
            return ServiceResult.Success("User activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user with ID {UserId}", id);
            return ServiceResult.InternalError("An error occurred while activating the user");
        }
    }

    public async Task<ServiceResult> DeactivateAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"User with ID {id} not found");
            }

            if (!user.IsActive)
            {
                return ServiceResult.Success("User is already inactive");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated user with ID {UserId}", id);
            return ServiceResult.Success("User deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user with ID {UserId}", id);
            return ServiceResult.InternalError("An error occurred while deactivating the user");
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
            return ServiceResult<bool>.InternalError("An error occurred while checking user existence");
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
            return ServiceResult<bool>.InternalError("An error occurred while checking email existence");
        }
    }

    public async Task<ServiceResult> UpdateLastLoginAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult.NotFound($"User with ID {id} not found");
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated last login for user {UserId}", id);
            return ServiceResult.Success("Last login updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {UserId}", id);
            return ServiceResult.InternalError("An error occurred while updating last login");
        }
    }

    public async Task<ServiceResult<int>> UpdateProfileCompletionAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return ServiceResult<int>.NotFound($"User with ID {id} not found");
            }

            user.CalculateProfileCompletion();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated profile completion for user {UserId}: {Completion}%", id, user.ProfileCompletionPercentage);
            return ServiceResult<int>.Success(user.ProfileCompletionPercentage, "Profile completion updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile completion for user {UserId}", id);
            return ServiceResult<int>.InternalError("An error occurred while updating profile completion");
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
            
            _logger.LogInformation("Found {Count} users with incomplete profiles (threshold: {Threshold}%)", userDtos.Count, threshold);
            return ServiceResult<List<UserSearchResultDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users with incomplete profiles");
            return ServiceResult<List<UserSearchResultDto>>.InternalError("An error occurred while retrieving users with incomplete profiles");
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
            return ServiceResult<List<UserSearchResultDto>>.InternalError("An error occurred while retrieving recently registered users");
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
            return ServiceResult<List<UserSearchResultDto>>.InternalError("An error occurred while retrieving inactive users");
        }
    }

    public async Task<ServiceResult<int>> BulkUpdateAsync(List<Guid> userIds, UpdateUserDto updates)
    {
        try
        {
            if (!userIds.Any())
            {
                return ServiceResult<int>.Success(0, "No users to update");
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
                return ServiceResult<int>.NotFound("No users found with the provided IDs");
            }

            int updatedCount = 0;
            foreach (var user in users)
            {
                try
                {
                    // Check email uniqueness if email is being changed
                    if (!string.IsNullOrEmpty(updates.Email) && updates.Email != user.Email)
                    {
                        var emailExists = await _context.Users.AnyAsync(u => u.Email == updates.Email && u.Id != user.Id);
                        if (emailExists)
                        {
                            _logger.LogWarning("Skipping user {UserId} - email already exists: {Email}", user.Id, updates.Email);
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
                _logger.LogInformation("Bulk updated {UpdatedCount} out of {RequestedCount} users", updatedCount, userIds.Count);
            }

            return ServiceResult<int>.Success(updatedCount, $"Successfully updated {updatedCount} users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update of users");
            return ServiceResult<int>.InternalError("An error occurred during bulk update");
        }
    }
    
    private IQueryable<User> BuildUserQuery(UserFilterDto filter)
    {
        var query = _context.Users
            .Include(u => u.Organization)
            .AsQueryable();

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
        
        // Search terms
        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            var searchTerms = filter.SearchTerms.ToLower();
            query = query.Where(u => 
                u.FirstName.ToLower().Contains(searchTerms) ||
                u.LastName.ToLower().Contains(searchTerms) ||
                u.Email!.ToLower().Contains(searchTerms) ||
                u.Address.City.ToLower().Contains(searchTerms) ||
                u.Address.Country.ToLower().Contains(searchTerms) ||
                u.Address.PostalCode.ToLower().Contains(searchTerms) ||
                u.Address.Region.ToLower().Contains(searchTerms) ||
                u.Address.Street.ToLower().Contains(searchTerms) ||
                u.Address.AddressLine2.ToLower().Contains(searchTerms) ||
                u.Address.StreetNumber.ToLower().Contains(searchTerms) ||
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
    

 
}