
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Pagination;

namespace XpertSphere.MonolithApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationsController(IOrganizationService organizationService) : ControllerBase
    {
        private readonly IOrganizationService _organizationService = organizationService;

        /// <summary>
        /// Get all organizations with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ResponseResource<Organization>>> GetOrganizations([FromQuery] OrganizationFilterDto filter)
        {
            return Ok(await _organizationService.GetAll(filter));
        }

        /// <summary>
        /// Get organization by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Organization>> GetOrganization(Guid id)
        {
            return Ok(await _organizationService.Get(id));
        }


        /// <summary>
        /// Add a new organization
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Organization>> AddOrganization(Organization organization)
        {

            var org = await _organizationService.Post(organization);
            return CreatedAtAction(nameof(GetOrganization), new { id = org.Id }, org);
        }

        /// <summary>
        /// Update an existing organization
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<Organization>> UpdateUser(Guid id, Organization organization)
        {


            var org = await _organizationService.Put(id, organization);

            return Ok(org);
        }

        /// <summary>
        /// Delete an organization
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _organizationService.Delete(id);
            return Ok();
        }
    }
}
