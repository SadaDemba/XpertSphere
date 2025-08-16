using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.JobOffer;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobOffersController(IJobOfferService jobOfferService) : ControllerBase
{
    /// <summary>
    /// Get all job offers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobOfferDto>>> GetAllJobOffers()
    {
        var result = await jobOfferService.GetAllJobOffersAsync();
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all job offers with pagination and filtering
    /// </summary>
    [HttpGet("paginated")]
    public async Task<ActionResult<PaginatedResult<JobOfferDto>>> GetAllPaginatedJobOffers([FromQuery] JobOfferFilterDto filter)
    {
        var result = await jobOfferService.GetAllPaginatedJobOffersAsync(filter);
        return this.ToPaginatedActionResult(result);
    }

    /// <summary>
    /// Get a job offer by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobOfferDto>> GetJobOfferById(Guid id)
    {
        var result = await jobOfferService.GetJobOfferByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Create a new job offer
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<JobOfferDto>> CreateJobOffer(CreateJobOfferDto createJobOfferDto)
    {
        var userId = this.GetCurrentUserId();
        var organizationId = this.GetCurrentUserOrganizationId();

        if (!userId.HasValue || !organizationId.HasValue)
        {
            return BadRequest("User ID or Organization ID not found in claims");
        }

        var result = await jobOfferService.CreateJobOfferAsync(createJobOfferDto, userId.Value, organizationId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update an existing job offer
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<JobOfferDto>> UpdateJobOffer(Guid id, UpdateJobOfferDto updateJobOfferDto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await jobOfferService.UpdateJobOfferAsync(id, updateJobOfferDto, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Delete a job offer
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult> DeleteJobOffer(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await jobOfferService.DeleteJobOfferAsync(id, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Publish a job offer
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult> PublishJobOffer(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await jobOfferService.PublishJobOfferAsync(id, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Close a job offer
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult> CloseJobOffer(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await jobOfferService.CloseJobOfferAsync(id, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get job offers by organization
    /// </summary>
    [HttpGet("organization/{organizationId:guid}")]
    [Authorize(Policy = "OrganizationAccess")]
    public async Task<ActionResult<IEnumerable<JobOfferDto>>> GetJobOffersByOrganization(Guid organizationId)
    {
        var result = await jobOfferService.GetJobOffersByOrganizationAsync(organizationId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get job offers created by current user
    /// </summary>
    [HttpGet("my")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<IEnumerable<JobOfferDto>>> GetMyJobOffers()
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await jobOfferService.GetJobOffersByUserAsync(userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if current user can manage a specific job offer
    /// </summary>
    [HttpGet("{id:guid}/can-manage")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<bool>> CanManageJobOffer(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await jobOfferService.CanUserManageJobOfferAsync(id, userId.Value);
        return this.ToActionResult(result);
    }
}