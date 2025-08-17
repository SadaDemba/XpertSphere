using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IApplicationStatusHistoryService
{
    Task<ServiceResult<ApplicationStatusHistoryDto>> AddStatusChangeAsync(AddStatusChangeDto addStatusChangeDto);
    Task<ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>> GetHistoryAsync(Guid applicationId);
}