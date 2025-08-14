using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IUserRoleService
{
    Task<ServiceResult<IEnumerable<UserRoleDto>>> GetUserRolesAsync(Guid userId);
    Task<ServiceResult<IEnumerable<UserRoleDto>>> GetRoleUsersAsync(Guid roleId);
    Task<ServiceResult<UserRoleDto>> AssignRoleToUserAsync(AssignRoleDto assignRoleDto);
    Task<ServiceResult> RemoveRoleFromUserAsync(Guid userRoleId);
    Task<ServiceResult> UpdateUserRoleStatusAsync(Guid userRoleId, bool isActive);
    Task<ServiceResult> ExtendUserRoleAsync(Guid userRoleId, DateTime? newExpiryDate);
    Task<ServiceResult<bool>> UserHasRoleAsync(Guid userId, string roleName);
    Task<ServiceResult<bool>> UserHasActiveRoleAsync(Guid userId, string roleName);
    Task<ServiceResult<IEnumerable<string>>> GetUserRoleNamesAsync(Guid userId);
}