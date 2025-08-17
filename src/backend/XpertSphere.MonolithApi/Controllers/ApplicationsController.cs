using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Application;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController(IApplicationService applicationService) : ControllerBase
{
    /// <summary>
    /// Get all applications
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetAllApplications()
    {
        var result = await applicationService.GetAllApplicationsAsync();
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get all applications with pagination and filtering
    /// </summary>
    [HttpGet("paginated")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<PaginatedResult<ApplicationDto>>> GetAllPaginatedApplications([FromQuery] ApplicationFilterDto filter)
    {
        var result = await applicationService.GetAllPaginatedApplicationsAsync(filter);
        return this.ToPaginatedActionResult(result);
    }

    /// <summary>
    /// Get an application by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationDto>> GetApplicationById(Guid id)
    {
        var result = await applicationService.GetApplicationByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Create a new application (apply to job offer)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> CreateApplication(CreateApplicationDto createApplicationDto)
    {
        var candidateId = this.GetCurrentUserId();
        if (!candidateId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.CreateApplicationAsync(createApplicationDto, candidateId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update an existing application
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationDto>> UpdateApplication(Guid id, UpdateApplicationDto updateApplicationDto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.UpdateApplicationAsync(id, updateApplicationDto, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Delete an application
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteApplication(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.DeleteApplicationAsync(id, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update application status (recruiter action)
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<ApplicationDto>> UpdateApplicationStatus(Guid id, UpdateApplicationStatusDto updateStatusDto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.UpdateApplicationStatusAsync(id, updateStatusDto, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Withdraw application (candidate action)
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult> WithdrawApplication(Guid id, [FromBody] WithdrawApplicationRequest request)
    {
        var candidateId = this.GetCurrentUserId();
        if (!candidateId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.WithdrawApplicationAsync(id, request.Reason, candidateId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get applications for a specific job offer
    /// </summary>
    [HttpGet("job-offer/{jobOfferId:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplicationsByJobOffer(Guid jobOfferId)
    {
        var result = await applicationService.GetApplicationsByJobOfferAsync(jobOfferId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get applications by current candidate
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetMyCandidateApplications()
    {
        var candidateId = this.GetCurrentUserId();
        if (!candidateId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.GetApplicationsByCandidateAsync(candidateId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get applications for a specific candidate (recruiter view)
    /// </summary>
    [HttpGet("candidate/{candidateId:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplicationsByCandidate(Guid candidateId)
    {
        var result = await applicationService.GetApplicationsByCandidateAsync(candidateId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get applications for current user's organization
    /// </summary>
    [HttpGet("organization")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplicationsByOrganization()
    {
        var organizationId = this.GetCurrentUserOrganizationId();
        if (!organizationId.HasValue)
        {
            return BadRequest("Organization ID not found in claims");
        }

        var result = await applicationService.GetApplicationsByOrganizationAsync(organizationId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get applications for a specific organization
    /// </summary>
    [HttpGet("organization/{organizationId:guid}")]
    [Authorize(Policy = "RequirePlatformRole")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplicationsByOrganization(Guid organizationId)
    {
        var result = await applicationService.GetApplicationsByOrganizationAsync(organizationId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get status history for an application
    /// </summary>
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IEnumerable<ApplicationStatusHistoryDto>>> GetApplicationStatusHistory(Guid id)
    {
        var result = await applicationService.GetApplicationStatusHistoryAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if current user can manage a specific application
    /// </summary>
    [HttpGet("{id:guid}/can-manage")]
    public async Task<ActionResult<bool>> CanManageApplication(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.CanUserManageApplicationAsync(id, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if candidate has already applied to a job offer
    /// </summary>
    [HttpGet("check-applied/job-offer/{jobOfferId:guid}")]
    public async Task<ActionResult<bool>> HasAppliedToJob(Guid jobOfferId)
    {
        var candidateId = this.GetCurrentUserId();
        if (!candidateId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        var result = await applicationService.HasCandidateAppliedToJobAsync(jobOfferId, candidateId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if a specific candidate has applied to a job offer (recruiter view)
    /// </summary>
    [HttpGet("check-applied/job-offer/{jobOfferId:guid}/candidate/{candidateId:guid}")]
    [Authorize(Policy = "RequireRecruiterRole")]
    public async Task<ActionResult<bool>> HasCandidateAppliedToJob(Guid jobOfferId, Guid candidateId)
    {
        var result = await applicationService.HasCandidateAppliedToJobAsync(jobOfferId, candidateId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Assign a user to an application (recruiter action)
    /// </summary>
    [HttpPost("{id:guid}/assign-user")]
    [Authorize(Policy = "CanAssignEvaluators")]
    public async Task<ActionResult<ApplicationDto>> AssignUser(Guid id, AssignUserDto assignUserDto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        // Ensure the application ID matches the route parameter
        assignUserDto.ApplicationId = id;

        var result = await applicationService.AssignUserAsync(assignUserDto, userId.Value);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Unassign a user from an application (recruiter action)
    /// </summary>
    [HttpPost("{id:guid}/unassign-user")]
    [Authorize(Policy = "CanAssignEvaluators")]
    public async Task<ActionResult<ApplicationDto>> UnassignUser(Guid id, AssignUserDto unassignUserDto)
    {
        var userId = this.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return BadRequest("User ID not found in claims");
        }

        // Ensure the application ID matches the route parameter
        unassignUserDto.ApplicationId = id;

        var result = await applicationService.UnassignUserAsync(unassignUserDto, userId.Value);
        return this.ToActionResult(result);
    }
}

/// <summary>
/// Request model for withdrawing application
/// </summary>
public class WithdrawApplicationRequest
{
    public required string Reason { get; set; }
}