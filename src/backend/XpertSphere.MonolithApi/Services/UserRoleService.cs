using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class UserRoleService : IUserRoleService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<AssignRoleDto> _assignRoleValidator;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<AssignRoleDto> assignRoleValidator,
        ILogger<UserRoleService> logger)
    {
        _context = context;
        _mapper = mapper;
        _assignRoleValidator = assignRoleValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<UserRoleDto>>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var userRoles = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Include(ur => ur.AssignedByUser)
                .Where(ur => ur.UserId == userId)
                .OrderBy(ur => ur.AssignedAt)
                .ToListAsync();

            var userRoleDtos = _mapper.Map<IEnumerable<UserRoleDto>>(userRoles);
            return ServiceResult<IEnumerable<UserRoleDto>>.Success(userRoleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user roles for user {UserId}", userId);
            return ServiceResult<IEnumerable<UserRoleDto>>.InternalError("An error occurred while retrieving user roles");
        }
    }

    public async Task<ServiceResult<IEnumerable<UserRoleDto>>> GetRoleUsersAsync(Guid roleId)
    {
        try
        {
            
            var roleUsers = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Include(ur => ur.AssignedByUser)
                .Where(ur => ur.RoleId == roleId && 
                            ur.IsActive && 
                            ur.User.IsActive &&
                            (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
                .OrderBy(ur => ur.User.FirstName)
                .ToListAsync();

            var roleUserDtos = _mapper.Map<IEnumerable<UserRoleDto>>(roleUsers);
            return ServiceResult<IEnumerable<UserRoleDto>>.Success(roleUserDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for role {RoleId}", roleId);
            return ServiceResult<IEnumerable<UserRoleDto>>.InternalError("An error occurred while retrieving role users");
        }
    }

    public async Task<ServiceResult<UserRoleDto>> AssignRoleToUserAsync(AssignRoleDto assignRoleDto)
    {
        try
        {
            var validationResult = await _assignRoleValidator.ValidateAsync(assignRoleDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<UserRoleDto>.ValidationError(errors);
            }
            // Check if user exists
            var user = await _context.Users.FindAsync(assignRoleDto.UserId);
            if (user == null)
            {
                return ServiceResult<UserRoleDto>.NotFound($"User with ID {assignRoleDto.UserId} not found");
            }

            // Check if the role exists
            var role = await _context.Roles.FindAsync(assignRoleDto.RoleId);
            if (role == null)
            {
                return ServiceResult<UserRoleDto>.NotFound($"Role with ID {assignRoleDto.RoleId} not found");
            }

            if (!role.IsActive)
            {
                return ServiceResult<UserRoleDto>.Failure("Cannot assign inactive role");
            }

            // Check if user already has this role (active)
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == assignRoleDto.UserId && 
                                          ur.RoleId == assignRoleDto.RoleId && 
                                          ur.IsActive);

            if (existingUserRole != null)
            {
                return ServiceResult<UserRoleDto>.Conflict("User already has this role assigned");
            }

            var userRole = _mapper.Map<UserRole>(assignRoleDto);
            userRole.Id = Guid.NewGuid();
            userRole.AssignedAt = DateTime.UtcNow;
            userRole.IsActive = true;

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned role {RoleName} to user {UserId}", role.Name, assignRoleDto.UserId);

            // Reload with includes for mapping
            var createdUserRole = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Include(ur => ur.AssignedByUser)
                .FirstOrDefaultAsync(ur => ur.Id == userRole.Id);

            var userRoleDto = _mapper.Map<UserRoleDto>(createdUserRole);
            return ServiceResult<UserRoleDto>.Success(userRoleDto, "Role assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", assignRoleDto.RoleId, assignRoleDto.UserId);
            return ServiceResult<UserRoleDto>.InternalError("An error occurred while assigning role to user");
        }
    }

    public async Task<ServiceResult> RemoveRoleFromUserAsync(Guid userRoleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.Id == userRoleId);
                
            if (userRole == null)
            {
                return ServiceResult.NotFound($"User role assignment with ID {userRoleId} not found");
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed role {RoleName} from user {UserId}", userRole.Role.Name, userRole.UserId);
            return ServiceResult.Success("Role removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user role with ID {UserRoleId}", userRoleId);
            return ServiceResult.InternalError("An error occurred while removing role from user");
        }
    }

    public async Task<ServiceResult> UpdateUserRoleStatusAsync(Guid userRoleId, bool isActive)
    {
        try
        {
            var userRole = await _context.UserRoles.FindAsync(userRoleId);
            if (userRole == null)
            {
                return ServiceResult.NotFound($"User role assignment with ID {userRoleId} not found");
            }

            if (userRole.IsActive == isActive)
            {
                return ServiceResult.Success($"User role is already {(isActive ? "active" : "inactive")}");
            }

            userRole.IsActive = isActive;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Action} user role with ID {UserRoleId}", isActive ? "Activated" : "Deactivated", userRoleId);
            return ServiceResult.Success($"User role {(isActive ? "activated" : "deactivated")} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for user role with ID {UserRoleId}", userRoleId);
            return ServiceResult.InternalError("An error occurred while updating user role status");
        }
    }

    public async Task<ServiceResult> ExtendUserRoleAsync(Guid userRoleId, DateTime? newExpiryDate)
    {
        try
        {
            var userRole = await _context.UserRoles.FindAsync(userRoleId);
            if (userRole == null)
            {
                return ServiceResult.NotFound($"User role assignment with ID {userRoleId} not found");
            }

            userRole.ExpiresAt = newExpiryDate;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Extended user role with ID {UserRoleId} to {ExpiryDate}", userRoleId, newExpiryDate);
            return ServiceResult.Success("User role expiry updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending user role with ID {UserRoleId}", userRoleId);
            return ServiceResult.InternalError("An error occurred while extending user role");
        }
    }

    public async Task<ServiceResult<bool>> UserHasRoleAsync(Guid userId, string roleName)
    {
        try
        {
            var hasRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && 
                               ur.Role.Name == roleName);

            return ServiceResult<bool>.Success(hasRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has role {RoleName}", userId, roleName);
            return ServiceResult<bool>.InternalError("An error occurred while checking user role");
        }
    }

    public async Task<ServiceResult<bool>> UserHasActiveRoleAsync(Guid userId, string roleName)
    {
        try
        {
            var hasActiveRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && 
                               ur.Role.Name == roleName &&
                               ur.IsActive &&
                               ur.Role.IsActive &&
                               (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow));

            return ServiceResult<bool>.Success(hasActiveRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has active role {RoleName}", userId, roleName);
            return ServiceResult<bool>.InternalError("An error occurred while checking active user role");
        }
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetUserRoleNamesAsync(Guid userId)
    {
        try
        {
            var roleNames = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && 
                            ur.IsActive && 
                            ur.Role.IsActive &&
                            (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return ServiceResult<IEnumerable<string>>.Success(roleNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role names for user {UserId}", userId);
            return ServiceResult<IEnumerable<string>>.InternalError("An error occurred while retrieving user role names");
        }
    }
}