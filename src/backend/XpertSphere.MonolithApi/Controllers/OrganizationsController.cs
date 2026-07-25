using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationsController(IOrganizationService organizationService) : ControllerBase
    {
        /// <summary>
        /// Get all organizations with filtering and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "RequirePlatformRole")]
        public async Task<ActionResult<PaginatedResult<OrganizationDto>>> GetOrganizations(
            [FromQuery] OrganizationFilterDto filter)
        {
            var result = await organizationService.GetAllAsync(filter);
            return this.ToPaginatedActionResult(result);
        }

        /// <summary>
        /// Get all organizations without pagination
        /// </summary>
        [HttpGet("all")]
        [Authorize(Policy = "RequireInternalUser")]
        public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetAllOrganizations()
        {
            var result = await organizationService.GetAllWithoutPaginationAsync();
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Get organization by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "OrganizationAccess")]
        public async Task<ActionResult<OrganizationDto>> GetOrganization(Guid id)
        {
            var result = await organizationService.GetByIdAsync(id);
            return this.ToActionResult(result);
        }


        /// <summary>
        /// Add a new organization
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "RequirePlatformSuperAdminRole")]
        public async Task<ActionResult<OrganizationDto>> AddOrganization(CreateOrganizationDto organization)
        {
            var result = await organizationService.CreateAsync(organization);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Update an existing organization
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "OrganizationAccess")]
        public async Task<ActionResult<OrganizationDto>> UpdateOrganization(Guid id, UpdateOrganizationDto organization)
        {
            var result = await organizationService.UpdateAsync(id, organization);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Delete an organization
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "RequirePlatformSuperAdminRole")]
        public async Task<IActionResult> DeleteOrganization(Guid id)
        {
            var result = await organizationService.DeleteAsync(id);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Get the currency configured for the caller's own organization
        /// </summary>
        [HttpGet("me/currency")]
        [Authorize(Policy = "RequireOrganizationAdminRole")]
        public async Task<ActionResult<OrganizationCurrencyDto>> GetMyOrganizationCurrency()
        {
            var organizationId = this.GetCurrentUserOrganizationId();
            if (organizationId is null)
            {
                return Forbid();
            }

            var result = await organizationService.GetCurrencyAsync(organizationId.Value);
            return this.ToActionResult(result);
        }

        /// <summary>
        /// Update the currency configured for the caller's own organization
        /// </summary>
        [HttpPut("me/currency")]
        [Authorize(Policy = "RequireOrganizationAdminRole")]
        public async Task<ActionResult<OrganizationCurrencyDto>> UpdateMyOrganizationCurrency(
            UpdateOrganizationCurrencyDto dto)
        {
            var organizationId = this.GetCurrentUserOrganizationId();
            if (organizationId is null)
            {
                return Forbid();
            }

            var result = await organizationService.UpdateCurrencyAsync(organizationId.Value, dto);
            return this.ToActionResult(result);
        }
    }
}