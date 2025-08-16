using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.RolePermission;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolePermissionsController(IRolePermissionService rolePermissionService) : ControllerBase
{
    /// <summary>
    /// Get all permissions for a specific role
    /// </summary>
    [HttpGet("role/{roleId:guid}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetRolePermissions(Guid roleId)
    {
        var result = await rolePermissionService.GetRolePermissionsAsync(roleId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all roles for a specific permission
    /// </summary>
    [HttpGet("permission/{permissionId:guid}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetPermissionRoles(Guid permissionId)
    {
        var result = await rolePermissionService.GetPermissionRolesAsync(permissionId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Assign a permission to a given role
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult<RolePermissionDto>> AssignPermissionToRole([FromBody] AssignPermissionDto assignPermissionDto)
    {
        var result = await rolePermissionService.AssignPermissionToRoleAsync(assignPermissionDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Remove a permission from a role
    /// </summary>
    [HttpDelete("{rolePermissionId:guid}")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult> RemovePermissionFromRole(Guid rolePermissionId)
    {
        var result = await rolePermissionService.RemovePermissionFromRoleAsync(rolePermissionId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if the role has a specific permission
    /// </summary>
    [HttpGet("role/{roleId:guid}/has-permission/{permissionName}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<bool>> CheckRoleHasPermission(Guid roleId, string permissionName)
    {
        var result = await rolePermissionService.RoleHasPermissionAsync(roleId, permissionName);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all permission names for a role
    /// </summary>
    [HttpGet("role/{roleId:guid}/permission-names")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<string>>> GetRolePermissionNames(Guid roleId)
    {
        var result = await rolePermissionService.GetRolePermissionNamesAsync(roleId);
        return this.ToActionResult(result);
    }
}