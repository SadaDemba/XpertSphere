using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.Permission;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Services;

public class PermissionService : IPermissionService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreatePermissionDto> _createValidator;
    private readonly IValidator<PermissionFilterDto> _filterValidator;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<CreatePermissionDto> createValidator,
        IValidator<PermissionFilterDto> filterValidator,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _createValidator = createValidator;
        _filterValidator = filterValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<PermissionDto>>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _context.Permissions
                .Include(p => p.RolePermissions)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync();

            var permissionDtos = _mapper.Map<IEnumerable<PermissionDto>>(permissions);
            return ServiceResult<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all permissions");
            return ServiceResult<IEnumerable<PermissionDto>>.InternalError("An error occurred while retrieving permissions");
        }
    }

    public async Task<PaginatedResult<PermissionDto>> GetAllPaginatedPermissionsAsync(PermissionFilterDto filter)
    {
        try
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return PaginatedResult<PermissionDto>.Failure(errors, "Invalid filter parameters");
            }

            var query = BuildPermissionQuery(filter);
            
            var pageNumber = int.TryParse(filter.PageNumber, out var pn) ? pn : 1;
            var pageSize = int.TryParse(filter.PageSize, out var ps) ? ps : 10;

            var paginatedResult = await query.ToPaginatedResultAsync(pageNumber, pageSize);
            
            return paginatedResult.Map(permission => _mapper.Map<PermissionDto>(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated permissions with filter {Filter}", filter);
            return PaginatedResult<PermissionDto>.Failure("An error occurred while searching permissions");
        }
    }


    public async Task<ServiceResult<PermissionDto>> GetPermissionByIdAsync(Guid id)
    {
        try
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (permission == null)
            {
                return ServiceResult<PermissionDto>.NotFound($"Permission with ID {id} not found");
            }

            var permissionDto = _mapper.Map<PermissionDto>(permission);
            return ServiceResult<PermissionDto>.Success(permissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission with ID {PermissionId}", id);
            return ServiceResult<PermissionDto>.InternalError("An error occurred while retrieving the permission");
        }
    }

    public async Task<ServiceResult<IEnumerable<PermissionDto>>> GetPermissionsByResourceAsync(string resource)
    {
        try
        {
            var permissions = await _context.Permissions
                .Include(p => p.RolePermissions)
                .Where(p => p.Resource == resource)
                .OrderBy(p => p.Action)
                .ToListAsync();

            var permissionDtos = _mapper.Map<IEnumerable<PermissionDto>>(permissions);
            return ServiceResult<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions by resource {Resource}", resource);
            return ServiceResult<IEnumerable<PermissionDto>>.InternalError("An error occurred while retrieving permissions by resource");
        }
    }

    public async Task<ServiceResult<IEnumerable<PermissionDto>>> GetPermissionsByCategoryAsync(string category)
    {
        try
        {
            var permissions = await _context.Permissions
                .Include(p => p.RolePermissions)
                .Where(p => p.Category == category)
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync();

            var permissionDtos = _mapper.Map<IEnumerable<PermissionDto>>(permissions);
            return ServiceResult<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions by category {Category}", category);
            return ServiceResult<IEnumerable<PermissionDto>>.InternalError("An error occurred while retrieving permissions by category");
        }
    }

    public async Task<ServiceResult<PermissionDto>> CreatePermissionAsync(CreatePermissionDto createPermissionDto)
    {
        try
        {
            var validationResult = await _createValidator.ValidateAsync(createPermissionDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<PermissionDto>.ValidationError(errors);
            }

            // Check if permission with the same name already exists
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == createPermissionDto.Name);

            if (existingPermission != null)
            {
                return ServiceResult<PermissionDto>.Conflict($"A permission with name '{createPermissionDto.Name}' already exists");
            }

            var permission = _mapper.Map<Permission>(createPermissionDto);
            permission.Id = Guid.NewGuid();

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new permission with ID {PermissionId} and name {PermissionName}", permission.Id, permission.Name);

            var permissionDto = _mapper.Map<PermissionDto>(permission);
            return ServiceResult<PermissionDto>.Success(permissionDto, "Permission created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission with name {PermissionName}", createPermissionDto.Name);
            return ServiceResult<PermissionDto>.InternalError("An error occurred while creating the permission");
        }
    }

    public async Task<ServiceResult> DeletePermissionAsync(Guid id)
    {
        try
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return ServiceResult.NotFound($"Permission with ID {id} not found");
            }

            // Check if permission is assigned to any roles
            var hasRoles = await _context.RolePermissions
                .AnyAsync(rp => rp.PermissionId == id);

            if (hasRoles)
            {
                return ServiceResult.Failure("Cannot delete permission that is assigned to roles");
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted permission with ID {PermissionId} and name {PermissionName}", id, permission.Name);
            return ServiceResult.Success("Permission deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission with ID {PermissionId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the permission");
        }
    }

    public async Task<ServiceResult<bool>> PermissionExistsAsync(string name)
    {
        try
        {
            var exists = await _context.Permissions.AnyAsync(p => p.Name == name);
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if permission exists with name {PermissionName}", name);
            return ServiceResult<bool>.InternalError("An error occurred while checking permission existence");
        }
    }

    public async Task<ServiceResult<bool>> CanDeletePermissionAsync(Guid id)
    {
        try
        {
            var hasRoles = await _context.RolePermissions
                .AnyAsync(rp => rp.PermissionId == id);
            
            return ServiceResult<bool>.Success(!hasRoles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if permission can be deleted with ID {PermissionId}", id);
            return ServiceResult<bool>.InternalError("An error occurred while checking if permission can be deleted");
        }
    }

    private IQueryable<Permission> BuildPermissionQuery(PermissionFilterDto filter)
    {
        var query = _context.Permissions
            .Include(p => p.RolePermissions)
            .AsQueryable();
        
        // Apply filters
        if (!string.IsNullOrEmpty(filter.Resource))
        {
            query = query.Where(p => p.Resource == filter.Resource);
        }
        
        if (filter.Action.HasValue)
        {
            query = query.Where(p => p.Action == filter.Action);
        }
        
        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(p => p.Category == filter.Category);
        }
        
        if (filter.Scope.HasValue)
        {
            query = query.Where(p => p.Scope == filter.Scope);
        }
        
        // Search terms
        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            var searchTermsLower = filter.SearchTerms.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchTermsLower) ||
                p.Resource.ToLower().Contains(searchTermsLower) ||
                p.Category!.ToLower().Contains(searchTermsLower) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTermsLower))
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
                ? query.OrderBy(p => p.Category).ThenBy(p => p.Resource).ThenBy(p => p.Action)
                : query.OrderByDescending(p => p.Category).ThenByDescending(p => p.Resource).ThenByDescending(p => p.Action);
        }

        return query;
    }
    
    private static IQueryable<Permission> ApplySorting(IQueryable<Permission> query, string sortBy, SortDirection sortDirection)
    {
        return sortBy.ToLower() switch
        {
            "name" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Name) 
                : query.OrderByDescending(p => p.Name),
            "resource" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Resource) 
                : query.OrderByDescending(p => p.Resource),
            "action" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Action) 
                : query.OrderByDescending(p => p.Action),
            "category" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Category) 
                : query.OrderByDescending(p => p.Category),
            "scope" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Scope) 
                : query.OrderByDescending(p => p.Scope),
            "createdat" => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.CreatedAt) 
                : query.OrderByDescending(p => p.CreatedAt),
            _ => sortDirection == SortDirection.Ascending 
                ? query.OrderBy(p => p.Category).ThenBy(p => p.Resource).ThenBy(p => p.Action)
                : query.OrderByDescending(p => p.Category).ThenByDescending(p => p.Resource).ThenByDescending(p => p.Action)
            };
    }
}