using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Permission;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAllPermissions()
    {
        var result = await _permissionService.GetAllPermissionsAsync();
        return this.ToActionResult(result);
    }
    
    /// <summary>
    /// Get all paginated permissions
    /// </summary>
    [HttpGet("paginated")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<PaginatedResult<PermissionDto>>> GetAllPermissions([FromQuery] PermissionFilterDto filter)
    {
        var result = await _permissionService.GetAllPaginatedPermissionsAsync(filter);
        return this.ToPaginatedActionResult(result);
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<PermissionDto>> GetPermissionById(Guid id)
    {
        var result = await _permissionService.GetPermissionByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] CreatePermissionDto createPermissionDto)
    {
        var result = await _permissionService.CreatePermissionAsync(createPermissionDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Delete permission
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        var result = await _permissionService.DeletePermissionAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if permission exists by name
    /// </summary>
    [HttpGet("exists/{name}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<bool>> CheckPermissionExists(string name)
    {
        var result = await _permissionService.PermissionExistsAsync(name);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if permission can be deleted
    /// </summary>
    [HttpGet("{id:guid}/can-delete")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    public async Task<ActionResult<bool>> CanDeletePermission(Guid id)
    {
        var result = await _permissionService.CanDeletePermissionAsync(id);
        return this.ToActionResult(result);
    }
}