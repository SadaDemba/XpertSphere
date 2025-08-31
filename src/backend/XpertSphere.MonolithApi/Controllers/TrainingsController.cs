using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.TrainingDtos;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrainingsController(ITrainingService trainingService) : ControllerBase
{
    /// <summary>
    /// Get training by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrainingDto>> GetTrainingById(Guid id)
    {
        var result = await trainingService.GetTrainingByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all trainings for a specific user
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<IEnumerable<TrainingDto>>> GetUserTrainings(Guid userId)
    {
        var result = await trainingService.GetUserTrainingsAsync(userId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Replace all trainings for a user with new ones
    /// </summary>
    [HttpPut("user/{userId:guid}/replace")]
    public async Task<ActionResult<IEnumerable<TrainingDto>>> ReplaceUserTrainings(
        Guid userId, 
        [FromBody] List<CreateTrainingDto> trainings)
    {
        var result = await trainingService.ReplaceUserTrainingsAsync(userId, trainings);
        return this.ToActionResult(result);
    }
}