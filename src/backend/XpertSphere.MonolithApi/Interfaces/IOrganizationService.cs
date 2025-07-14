using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Pagination;

namespace XpertSphere.MonolithApi.Interfaces
{
    public interface IOrganizationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        Task<Organization> Get(Guid organizationId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizationFilterDto"></param>
        /// <returns></returns>
        Task<ResponseResource<Organization>> GetAll(OrganizationFilterDto organizationFilterDto);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="organization"></param>
        /// <returns></returns>
        Task<Organization> Post(Organization organization);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="organization"></param>
        /// <returns></returns>
        Task<Organization> Put(Guid id, Organization organization);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> Delete(Guid id);

    }
}
