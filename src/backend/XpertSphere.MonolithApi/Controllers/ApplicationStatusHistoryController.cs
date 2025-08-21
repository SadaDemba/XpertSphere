using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationStatusHistoryController(IApplicationStatusHistoryService statusHistoryService) : ControllerBase
{
    /// <summary>
    /// Get all status history entries for a specific application
    /// </summary>
    [HttpGet("application/{applicationId:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<IEnumerable<ApplicationStatusHistoryDto>>> GetByApplicationId(Guid applicationId)
    {
        var result = await statusHistoryService.GetByApplicationIdAsync(applicationId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get a specific status history entry by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<ApplicationStatusHistoryDto>> GetById(Guid id)
    {
        var result = await statusHistoryService.GetByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Create a new status history entry
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<ApplicationStatusHistoryDto>> Create([FromBody] CreateApplicationStatusHistoryDto dto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await statusHistoryService.CreateAsync(dto, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update an existing status history entry
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<ApplicationStatusHistoryDto>> Update(Guid id, [FromBody] UpdateApplicationStatusHistoryDto dto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await statusHistoryService.UpdateAsync(id, dto, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Delete a status history entry
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await statusHistoryService.DeleteAsync(id);
        return this.ToActionResult(result);
    }
}