using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IRoleService
{
    Task<ServiceResult<IEnumerable<RoleDto>>> GetAllRolesAsync();
    Task<PaginatedResult<RoleDto>> GetAllPaginatedRolesAsync(RoleFilterDto filter);
    Task<ServiceResult<RoleDto>> GetRoleByIdAsync(Guid id);
    Task<ServiceResult<RoleDto>> GetRoleByNameAsync(string name);
    Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleDto createRoleDto);
    Task<ServiceResult<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleDto updateRoleDto);
    Task<ServiceResult> DeleteRoleAsync(Guid id);
    Task<ServiceResult> ActivateRoleAsync(Guid id);
    Task<ServiceResult> DeactivateRoleAsync(Guid id);
    Task<ServiceResult<bool>> RoleExistsAsync(string name);
    Task<ServiceResult<bool>> CanDeleteRoleAsync(Guid id);
}