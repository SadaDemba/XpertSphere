using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Interfaces;

/// <summary>
/// Service interface for role management operations
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Get all roles without pagination
    /// </summary>
    Task<ServiceResult<IEnumerable<RoleDto>>> GetAllRolesAsync();
    
    /// <summary>
    /// Get roles with filters and pagination
    /// </summary>
    Task<PaginatedResult<RoleDto>> GetAllPaginatedRolesAsync(RoleFilterDto filter);
    
    /// <summary>
    /// Get a specific role by ID
    /// </summary>
    Task<ServiceResult<RoleDto>> GetRoleByIdAsync(Guid id);
    
    /// <summary>
    /// Get a specific role by name
    /// </summary>
    Task<ServiceResult<RoleDto>> GetRoleByNameAsync(string name);
    
    /// <summary>
    /// Create a new role
    /// </summary>
    Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto);
    
    /// <summary>
    /// Update an existing role
    /// </summary>
    Task<ServiceResult<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto);
    
    /// <summary>
    /// Delete a role (only if no active users assigned)
    /// </summary>
    Task<ServiceResult> DeleteRoleAsync(Guid id);
    
    /// <summary>
    /// Activate a deactivated role
    /// </summary>
    Task<ServiceResult> ActivateRoleAsync(Guid id);
    
    /// <summary>
    /// Deactivate an active role
    /// </summary>
    Task<ServiceResult> DeactivateRoleAsync(Guid id);
    
    /// <summary>
    /// Check if a role exists by name
    /// </summary>
    Task<ServiceResult<bool>> RoleExistsAsync(string name);
    
    /// <summary>
    /// Check if a role can be safely deleted (no active users)
    /// </summary>
    Task<ServiceResult<bool>> CanDeleteRoleAsync(Guid id);
}