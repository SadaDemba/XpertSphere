using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces
{
    public interface IOrganizationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        Task<ServiceResult<OrganizationDto>> GetByIdAsync(Guid organizationId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizationFilterDto"></param>
        /// <returns></returns>
        Task<PaginatedResult<OrganizationDto>> GetAllAsync(OrganizationFilterDto organizationFilterDto);

        /// <summary>
        /// Get all organizations without pagination
        /// </summary>
        /// <returns></returns>
        Task<ServiceResult<IEnumerable<OrganizationDto>>> GetAllWithoutPaginationAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="organization"></param>
        /// <returns></returns>
        Task<ServiceResult<OrganizationDto>> CreateAsync(CreateOrganizationDto organization);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateOrganizationDto"></param>
        /// <returns></returns>
        Task<ServiceResult<OrganizationDto>> UpdateAsync(Guid id, UpdateOrganizationDto updateOrganizationDto);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ServiceResult> DeleteAsync(Guid id);

    }
}
