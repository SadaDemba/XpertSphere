using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserRolesController : ControllerBase
{
    private readonly IUserRoleService _userRoleService;

    public UserRolesController(IUserRoleService userRoleService)
    {
        _userRoleService = userRoleService;
    }

    /// <summary>
    /// Get all roles for a specific user
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRoles(Guid userId)
    {
        var result = await _userRoleService.GetUserRolesAsync(userId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all users for a specific role
    /// </summary>
    [HttpGet("role/{roleId:guid}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetRoleUsers(Guid roleId)
    {
        var result = await _userRoleService.GetRoleUsersAsync(roleId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult<UserRoleDto>> AssignRoleToUser([FromBody] AssignRoleDto assignRoleDto)
    {
        var result = await _userRoleService.AssignRoleToUserAsync(assignRoleDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    [HttpDelete("{userRoleId:guid}")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult> RemoveRoleFromUser(Guid userRoleId)
    {
        var result = await _userRoleService.RemoveRoleFromUserAsync(userRoleId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update user role status (activate/deactivate)
    /// </summary>
    [HttpPatch("{userRoleId:guid}/status")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult> UpdateUserRoleStatus(Guid userRoleId, [FromBody] bool isActive)
    {
        var result = await _userRoleService.UpdateUserRoleStatusAsync(userRoleId, isActive);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Extend user role expiry date
    /// </summary>
    [HttpPatch("{userRoleId:guid}/extend")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult> ExtendUserRole(Guid userRoleId, [FromBody] DateTime? newExpiryDate)
    {
        var result = await _userRoleService.ExtendUserRoleAsync(userRoleId, newExpiryDate);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    [HttpGet("user/{userId:guid}/has-role/{roleName}")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult<bool>> CheckUserHasRole(Guid userId, string roleName)
    {
        var result = await _userRoleService.UserHasRoleAsync(userId, roleName);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if user has an active specific role
    /// </summary>
    [HttpGet("user/{userId:guid}/has-active-role/{roleName}")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult<bool>> CheckUserHasActiveRole(Guid userId, string roleName)
    {
        var result = await _userRoleService.UserHasActiveRoleAsync(userId, roleName);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all active role names for a user
    /// </summary>
    [HttpGet("user/{userId:guid}/role-names")]
    [Authorize(Policy = "RequireInternalUser")]
    public async Task<ActionResult<IEnumerable<string>>> GetUserRoleNames(Guid userId)
    {
        var result = await _userRoleService.GetUserRoleNamesAsync(userId);
        return this.ToActionResult(result);
    }
}