using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
    {
        var result = await roleService.GetAllRolesAsync();
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all roles with pagination
    /// </summary>
    [HttpGet("paginated")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<PaginatedResult<RoleDto>>> GetAllPaginatedRoles([FromQuery] RoleFilterDto filter)
    {
        var result = await roleService.GetAllPaginatedRolesAsync(filter);
        return this.ToPaginatedActionResult(result);
    }

    
    /// <summary>
    /// Get a role by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<RoleDto>> GetRoleById(Guid id)
    {
        var result = await roleService.GetRoleByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get a role by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<RoleDto>> GetRoleByName(string name)
    {
        var result = await roleService.GetRoleByNameAsync(name);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        var result = await roleService.CreateRoleAsync(createRoleDto);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleDto updateRoleDto)
    {
        var result = await roleService.UpdateRoleAsync(id, updateRoleDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult> DeleteRole(Guid id)
    {
        var result = await roleService.DeleteRoleAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Activate role
    /// </summary>
    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult> ActivateRole(Guid id)
    {
        var result = await roleService.ActivateRoleAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Deactivate role
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult> DeactivateRole(Guid id)
    {
        var result = await roleService.DeactivateRoleAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if role exists by name
    /// </summary>
    [HttpGet("exists/{name}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<bool>> CheckRoleExists(string name)
    {
        var result = await roleService.RoleExistsAsync(name);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if role can be deleted
    /// </summary>
    [HttpGet("{id:guid}/can-delete")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult<bool>> CanDeleteRole(Guid id)
    {
        var result = await roleService.CanDeleteRoleAsync(id);
        return this.ToActionResult(result);
    }
}