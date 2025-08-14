using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.Utils;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Extensions;

/// <summary>
/// Extension methods for controllers to handle ServiceResult responses
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Convert ServiceResult to ActionResult
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return result.StatusCode switch
            {
                200 => controller.Ok(result.Data),
                201 => controller.Created("", result.Data),
                202 => controller.Accepted(result.Data),
                204 => controller.NoContent(),
                _ => controller.Ok(result.Data)
            };

        return result.StatusCode switch
        {
            400 => controller.BadRequest(new { message = result.Message, errors = result.Errors }),
            401 => controller.Unauthorized(new { message = result.Message, errors = result.Errors }),
            403 => controller.Forbid(),
            404 => controller.NotFound(new { message = result.Message, errors = result.Errors }),
            409 => controller.Conflict(new { message = result.Message, errors = result.Errors }),
            422 => controller.UnprocessableEntity(new { message = result.Message, errors = result.Errors }),
            500 => controller.StatusCode(500, new { message = result.Message, errors = result.Errors }),
            _ => controller.BadRequest(new { message = result.Message, errors = result.Errors })
        };
    }

    /// <summary>
    /// Convert ServiceResult (non-generic) to ActionResult
    /// </summary>
    public static ActionResult ToActionResult(this ControllerBase controller, ServiceResult result)
    {
        if (result.IsSuccess)
            return result.StatusCode switch
            {
                200 => controller.Ok(new { message = result.Message }),
                201 => controller.Created("", new { message = result.Message }),
                202 => controller.Accepted(new { message = result.Message }),
                204 => controller.NoContent(),
                _ => controller.Ok(new { message = result.Message })
            };

        return result.StatusCode switch
        {
            400 => controller.BadRequest(new { message = result.Message, errors = result.Errors }),
            401 => controller.Unauthorized(new { message = result.Message, errors = result.Errors }),
            403 => controller.Forbid(),
            404 => controller.NotFound(new { message = result.Message, errors = result.Errors }),
            409 => controller.Conflict(new { message = result.Message, errors = result.Errors }),
            422 => controller.UnprocessableEntity(new { message = result.Message, errors = result.Errors }),
            500 => controller.StatusCode(500, new { message = result.Message, errors = result.Errors }),
            _ => controller.BadRequest(new { message = result.Message, errors = result.Errors })
        };
    }

    /// <summary>
    /// Convert PaginatedResult to ActionResult with pagination headers
    /// </summary>
    public static ActionResult<PaginatedResult<T>> ToPaginatedActionResult<T>(
        this ControllerBase controller,
        PaginatedResult<T> result,
        string? baseUrl = null)
    {
        if (!result.IsSuccess) return controller.BadRequest(new { message = result.Message, errors = result.Errors });

        // Add pagination headers
        controller.Response.Headers.Append("X-Pagination-Current-Page", result.Pagination.CurrentPage.ToString());
        controller.Response.Headers.Append("X-Pagination-Page-Size", result.Pagination.PageSize.ToString());
        controller.Response.Headers.Append("X-Pagination-Total-Items", result.Pagination.TotalItems.ToString());
        controller.Response.Headers.Append("X-Pagination-Total-Pages", result.Pagination.TotalPages.ToString());
        controller.Response.Headers.Append("X-Pagination-Has-Previous", result.Pagination.HasPrevious.ToString());
        controller.Response.Headers.Append("X-Pagination-Has-Next", result.Pagination.HasNext.ToString());

        // Add pagination links if base URL is provided
        if (string.IsNullOrEmpty(baseUrl)) return controller.Ok(result);

        var links = result.Pagination.GetLinks(baseUrl);

        if (!string.IsNullOrEmpty(links.First))
            controller.Response.Headers.Append("X-Pagination-Link-First", links.First);
        if (!string.IsNullOrEmpty(links.Previous))
            controller.Response.Headers.Append("X-Pagination-Link-Previous", links.Previous);
        if (!string.IsNullOrEmpty(links.Next))
            controller.Response.Headers.Append("X-Pagination-Link-Next", links.Next);
        if (!string.IsNullOrEmpty(links.Last))
            controller.Response.Headers.Append("X-Pagination-Link-Last", links.Last);

        return controller.Ok(result);
    }

    /// <summary>
    /// Get the current user ID from claims
    /// </summary>
    public static Guid? GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Get the current user's organization ID from claims
    /// </summary>
    private static Guid? GetCurrentUserOrganizationId(this ControllerBase controller)
    {
        var orgIdClaim = controller.User.FindFirst("OrganizationId");
        return Guid.TryParse(orgIdClaim?.Value, out var orgId) ? orgId : null;
    }

    /// <summary>
    /// Check if the current user has a specific role
    /// </summary>
    private static bool HasRole(this ControllerBase controller, string role)
    {
        return controller.User.IsInRole(role) || controller.User.HasClaim("roles", role);
    }

    /// <summary>
    /// Check if the current user is a super admin
    /// </summary>
    private static bool IsSuperAdmin(this ControllerBase controller)
    {
        return controller.HasRole(Roles.PlatformSuperAdmin.Name);
    }

    /// <summary>
    /// Check if the current user is an organization admin
    /// </summary>
    private static bool IsOrganizationAdmin(this ControllerBase controller)
    {
        return controller.HasRole(Roles.OrganizationAdmin.Name) || controller.IsSuperAdmin();
    }

    /// <summary>
    /// Check if the current user is a manager
    /// </summary>
    private static bool IsManager(this ControllerBase controller)
    {
        return controller.HasRole(Roles.Manager.Name) || controller.IsOrganizationAdmin();
    }

    /// <summary>
    /// Check if the current user is a recruiter
    /// </summary>
    private static bool IsRecruiter(this ControllerBase controller)
    {
        return controller.HasRole(Roles.Recruiter.Name) || controller.IsManager();
    }

    /// <summary>
    /// Check if the current user is a technical evaluator
    /// </summary>
    public static bool IsTechnicalEvaluator(this ControllerBase controller)
    {
        return controller.HasRole(Roles.TechnicalEvaluator.Name) || controller.IsRecruiter();
    }

    /// <summary>
    /// Check if the current user is an internal user (has organization roles)
    /// </summary>
    public static bool IsInternalUser(this ControllerBase controller)
    {
        return Roles.InternalRoles.Any(role => controller.HasRole(role));
    }

    /// <summary>
    /// Check if the current user can manage other users
    /// </summary>
    public static bool CanManageUsers(this ControllerBase controller)
    {
        return Roles.ManagementRoles.Any(role => controller.HasRole(role));
    }

    /// <summary>
    /// Check if the current user can recruit (manage job postings and candidates)
    /// </summary>
    public static bool CanRecruit(this ControllerBase controller)
    {
        return Roles.RecruitmentRoles.Any(role => controller.HasRole(role));
    }

    /// <summary>
    /// Check if the current user can evaluate candidates
    /// </summary>
    public static bool CanEvaluate(this ControllerBase controller)
    {
        return Roles.EvaluationRoles.Any(role => controller.HasRole(role));
    }

    /// <summary>
    /// Check if the current user can access a specific user's data
    /// </summary>
    public static bool CanAccessUser(this ControllerBase controller, Guid targetUserId)
    {
        var currentUserId = controller.GetCurrentUserId();

        // Super admin can access anyone
        if (controller.IsSuperAdmin())
            return true;

        // User can access their own data
        if (currentUserId == targetUserId)
            return true;

        // Organization admin/manager can access users in their organization
        var currentUserOrgId = controller.GetCurrentUserOrganizationId();
        if (currentUserOrgId.HasValue && (controller.IsOrganizationAdmin() || controller.IsManager()))
            return true;

        return false;
    }
}