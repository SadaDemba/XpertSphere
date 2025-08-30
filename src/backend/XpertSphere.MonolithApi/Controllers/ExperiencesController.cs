using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExperiencesController(IExperienceService experienceService) : ControllerBase
{
    /// <summary>
    /// Get experience by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExperienceDto>> GetExperienceById(Guid id)
    {
        var result = await experienceService.GetExperienceByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all experiences for a specific user
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<ExperienceDto>>> GetUserExperiences(Guid userId)
    {
        var result = await experienceService.GetUserExperiencesAsync(userId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Replace all experiences for a user with new ones
    /// </summary>
    [HttpPut("user/{userId:guid}/replace")]
    public async Task<ActionResult<IEnumerable<ExperienceDto>>> ReplaceUserExperiences(
        Guid userId, 
        [FromBody] List<CreateExperienceDto> experiences)
    {
        var result = await experienceService.ReplaceUserExperiencesAsync(userId, experiences);
        return this.ToActionResult(result);
    }
}