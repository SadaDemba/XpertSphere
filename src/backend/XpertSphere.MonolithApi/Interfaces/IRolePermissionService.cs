using XpertSphere.MonolithApi.DTOs.RolePermission;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IRolePermissionService
{
    Task<ServiceResult<IEnumerable<RolePermissionDto>>> GetRolePermissionsAsync(Guid roleId);
    Task<ServiceResult<IEnumerable<RolePermissionDto>>> GetPermissionRolesAsync(Guid permissionId);
    Task<ServiceResult<RolePermissionDto>> AssignPermissionToRoleAsync(AssignPermissionDto assignPermissionDto);
    Task<ServiceResult> RemovePermissionFromRoleAsync(Guid rolePermissionId);
    Task<ServiceResult<bool>> RoleHasPermissionAsync(Guid roleId, string permissionName);
    Task<ServiceResult<IEnumerable<string>>> GetRolePermissionNamesAsync(Guid roleId);
}