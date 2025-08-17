using XpertSphere.MonolithApi.DTOs.Application;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IApplicationService
{
    Task<ServiceResult<IEnumerable<ApplicationDto>>> GetAllApplicationsAsync();
    Task<PaginatedResult<ApplicationDto>> GetAllPaginatedApplicationsAsync(ApplicationFilterDto filter);
    Task<ServiceResult<ApplicationDto>> GetApplicationByIdAsync(Guid id);
    Task<ServiceResult<ApplicationDto>> CreateApplicationAsync(CreateApplicationDto createApplicationDto, Guid candidateId);
    Task<ServiceResult<ApplicationDto>> UpdateApplicationAsync(Guid id, UpdateApplicationDto updateApplicationDto, Guid userId);
    Task<ServiceResult> DeleteApplicationAsync(Guid id, Guid userId);
    Task<ServiceResult<ApplicationDto>> UpdateApplicationStatusAsync(Guid id, UpdateApplicationStatusDto updateStatusDto, Guid userId);
    Task<ServiceResult> WithdrawApplicationAsync(Guid id, string reason, Guid candidateId);
    Task<ServiceResult<IEnumerable<ApplicationDto>>> GetApplicationsByJobOfferAsync(Guid jobOfferId);
    Task<ServiceResult<IEnumerable<ApplicationDto>>> GetApplicationsByCandidateAsync(Guid candidateId);
    Task<ServiceResult<IEnumerable<ApplicationDto>>> GetApplicationsByOrganizationAsync(Guid organizationId);
    Task<ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>> GetApplicationStatusHistoryAsync(Guid applicationId);
    Task<ServiceResult<bool>> CanUserManageApplicationAsync(Guid applicationId, Guid userId);
    Task<ServiceResult<bool>> HasCandidateAppliedToJobAsync(Guid jobOfferId, Guid candidateId);
    Task<ServiceResult<ApplicationDto>> AssignUserAsync(AssignUserDto assignUserDto, Guid assignedByUserId);
    Task<ServiceResult<ApplicationDto>> UnassignUserAsync(AssignUserDto unassignUserDto, Guid assignedByUserId);
}