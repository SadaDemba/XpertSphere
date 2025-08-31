using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Services;

public class RoleService : IRoleService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateRoleDto> _createRoleValidator;
    private readonly IValidator<UpdateRoleDto> _updateRoleValidator;
    private readonly IValidator<RoleFilterDto> _filterValidator;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        XpertSphereDbContext context, 
        IMapper mapper,
        IValidator<CreateRoleDto> createRoleValidator,
        IValidator<UpdateRoleDto> updateRoleValidator,
        IValidator<RoleFilterDto> filterValidator,
        ILogger<RoleService> logger)
    {
        _context = context;
        _mapper = mapper;
        _createRoleValidator = createRoleValidator;
        _updateRoleValidator = updateRoleValidator;
        _filterValidator = filterValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<RoleDto>>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(roles);
            return ServiceResult<IEnumerable<RoleDto>>.Success(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles");
            return ServiceResult<IEnumerable<RoleDto>>.InternalError("An error occurred while retrieving roles");
        }
    }

    
    public async Task<PaginatedResult<RoleDto>> GetAllPaginatedRolesAsync(RoleFilterDto filter)
    {
        try
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return PaginatedResult<RoleDto>.Failure(errors, "Invalid filter parameters");
            }

            var query = BuildRoleQuery(filter);
            
            var pageNumber = int.TryParse(filter.PageNumber, out var pn) ? pn : 1;
            var pageSize = int.TryParse(filter.PageSize, out var ps) ? ps : 10;

            var paginatedResult = await query.ToPaginatedResultAsync(pageNumber, pageSize);
            
            return paginatedResult.Map(role => _mapper.Map<RoleDto>(role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated roles with filter {Filter}", filter);
            return PaginatedResult<RoleDto>.Failure("An error occurred while searching roles");
        }
    }

    public async Task<ServiceResult<RoleDto>> GetRoleByIdAsync(Guid id)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return ServiceResult<RoleDto>.NotFound($"Role with ID {id} not found");
            }
            
            var roleDto = _mapper.Map<RoleDto>(role);
            return ServiceResult<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with ID {RoleId}", id);
            return ServiceResult<RoleDto>.InternalError("An error occurred while retrieving the role");
        }
    }

    public async Task<ServiceResult<RoleDto>> GetRoleByNameAsync(string name)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .Where(r => r.Name == name)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return ServiceResult<RoleDto>.NotFound($"Role with name '{name}' not found");
            }
            
            var roleDto = _mapper.Map<RoleDto>(role);
            return ServiceResult<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with name {RoleName}", name);
            return ServiceResult<RoleDto>.InternalError("An error occurred while retrieving the role");
        }
    }

    public async Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        try
        {
            var validationResult = await _createRoleValidator.ValidateAsync(createRoleDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<RoleDto>.ValidationError(errors);
            }

            // Check if role with the same name already exists
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == createRoleDto.Name);

            if (existingRole != null)
            {
                return ServiceResult<RoleDto>.Conflict($"A role with name '{createRoleDto.Name}' already exists");
            }

            var role = _mapper.Map<Role>(createRoleDto);
            role.Id = Guid.NewGuid();
            role.IsActive = true;

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new role with ID {RoleId} and name {RoleName}", role.Id, role.Name);

            var roleDto = _mapper.Map<RoleDto>(role);
            return ServiceResult<RoleDto>.Success(roleDto, "Role created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role with name {RoleName}", createRoleDto.Name);
            return ServiceResult<RoleDto>.InternalError("An error occurred while creating the role");
        }
    }

    public async Task<ServiceResult<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto)
    {
        try
        {
            var validationResult = await _updateRoleValidator.ValidateAsync(updateRoleDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<RoleDto>.ValidationError(errors);
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return ServiceResult<RoleDto>.NotFound($"Role with ID {id} not found");
            }

            _mapper.Map(updateRoleDto, role);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated role with ID {RoleId}", id);

            var roleDto = _mapper.Map<RoleDto>(role);
            return ServiceResult<RoleDto>.Success(roleDto, "Role updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role with ID {RoleId}", id);
            return ServiceResult<RoleDto>.InternalError("An error occurred while updating the role");
        }
    }

    public async Task<ServiceResult> DeleteRoleAsync(Guid id)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return ServiceResult.NotFound($"Role with ID {id} not found");
            }

            // Check if the role has active users
            var hasActiveUsers = await _context.UserRoles
                .AnyAsync(ur => ur.RoleId == id && ur.IsActive);

            if (hasActiveUsers)
            {
                return ServiceResult.Failure("Cannot delete role that has active users assigned");
            }

            // Remove role permissions first
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToListAsync();
            
            _context.RolePermissions.RemoveRange(rolePermissions);

            // Remove user roles
            var userRoles = await _context.UserRoles
                .Where(ur => ur.RoleId == id)
                .ToListAsync();
            
            _context.UserRoles.RemoveRange(userRoles);

            // Remove role
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted role with ID {RoleId} and name {RoleName}", id, role.Name);
            return ServiceResult.Success("Role deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with ID {RoleId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the role");
        }
    }

    public async Task<ServiceResult> ActivateRoleAsync(Guid id)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return ServiceResult.NotFound($"Role with ID {id} not found");
            }

            if (role.IsActive)
            {
                return ServiceResult.Success("Role is already active");
            }

            role.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Activated role with ID {RoleId}", id);
            return ServiceResult.Success("Role activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating role with ID {RoleId}", id);
            return ServiceResult.InternalError("An error occurred while activating the role");
        }
    }

    public async Task<ServiceResult> DeactivateRoleAsync(Guid id)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return ServiceResult.NotFound($"Role with ID {id} not found");
            }

            if (!role.IsActive)
            {
                return ServiceResult.Success("Role is already inactive");
            }

            role.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated role with ID {RoleId}", id);
            return ServiceResult.Success("Role deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating role with ID {RoleId}", id);
            return ServiceResult.InternalError("An error occurred while deactivating the role");
        }
    }

    public async Task<ServiceResult<bool>> RoleExistsAsync(string name)
    {
        try
        {
            var exists = await _context.Roles.AnyAsync(r => r.Name == name);
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role exists with name {RoleName}", name);
            return ServiceResult<bool>.InternalError("An error occurred while checking role existence");
        }
    }

    public async Task<ServiceResult<bool>> CanDeleteRoleAsync(Guid id)
    {
        try
        {
            var hasActiveUsers = await _context.UserRoles
                .AnyAsync(ur => ur.RoleId == id && ur.IsActive);
            
            return ServiceResult<bool>.Success(!hasActiveUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role can be deleted with ID {RoleId}", id);
            return ServiceResult<bool>.InternalError("An error occurred while checking if role can be deleted");
        }
    }
    
    private IQueryable<Role> BuildRoleQuery(RoleFilterDto filter)
    {
        var query = _context.Roles
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .AsQueryable();
        
        // Apply filters
        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive);
        }
        
        if (filter.UserId.HasValue)
        {
            query = query.Where(r => r.UserRoles.Any(ur => ur.User.Id == filter.UserId));
        }
        
        // Search terms
        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            var searchTerms = filter.SearchTerms.ToLower();
            query = query.Where(r => 
                r.Name.ToLower().Contains(searchTerms) ||
                r.DisplayName.ToLower().Contains(searchTerms) ||
                r.Description!.ToLower().Contains(searchTerms)
                );
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
    
    private static IQueryable<Role> ApplySorting(IQueryable<Role> query, string sortBy, SortDirection sortDirection)
    {
        return sortBy.ToLower() switch
        {
            "name" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(r => r.Name) 
                : query.OrderByDescending(r => r.Name),
            "displayname" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(r => r.DisplayName) 
                : query.OrderByDescending(r => r.DisplayName),
            "createdat" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(r => r.CreatedAt) 
                : query.OrderByDescending(r => r.CreatedAt),
            _ => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(r => r.CreatedAt) 
                : query.OrderByDescending(u => u.CreatedAt)
            };
    }
}