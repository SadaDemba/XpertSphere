using XpertSphere.MonolithApi.DTOs.JobOffer;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Interfaces;
public interface IJobOfferService
{
    Task<ServiceResult<IEnumerable<JobOfferDto>>> GetAllJobOffersAsync();
    Task<PaginatedResult<JobOfferDto>> GetAllPaginatedJobOffersAsync(JobOfferFilterDto filter);
    Task<ServiceResult<JobOfferDto>> GetJobOfferByIdAsync(Guid id);
    Task<ServiceResult<JobOfferDto>> CreateJobOfferAsync(CreateJobOfferDto createJobOfferDto, Guid userId, Guid organizationId);
    Task<ServiceResult<JobOfferDto>> UpdateJobOfferAsync(Guid id, UpdateJobOfferDto updateJobOfferDto, Guid userId);
    Task<ServiceResult> DeleteJobOfferAsync(Guid id, Guid userId);
    Task<ServiceResult> PublishJobOfferAsync(Guid id, Guid userId);
    Task<ServiceResult> CloseJobOfferAsync(Guid id, Guid userId);
    Task<ServiceResult<IEnumerable<JobOfferDto>>> GetJobOffersByOrganizationAsync(Guid organizationId);
    Task<ServiceResult<IEnumerable<JobOfferDto>>> GetJobOffersByUserAsync(Guid userId);
    Task<ServiceResult<bool>> CanUserManageJobOfferAsync(Guid jobOfferId, Guid userId);
}