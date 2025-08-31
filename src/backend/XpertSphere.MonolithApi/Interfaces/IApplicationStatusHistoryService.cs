using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IApplicationStatusHistoryService
{
    Task<ServiceResult<ApplicationStatusHistoryDto>> AddStatusChangeAsync(AddStatusChangeDto addStatusChangeDto);
    Task<ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>> GetByApplicationIdAsync(Guid applicationId);
    Task<ServiceResult<ApplicationStatusHistoryDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<ApplicationStatusHistoryDto>> CreateAsync(CreateApplicationStatusHistoryDto dto, Guid updatedByUserId);
    Task<ServiceResult<ApplicationStatusHistoryDto>> UpdateAsync(Guid id, UpdateApplicationStatusHistoryDto dto, Guid updatedByUserId);
    Task<ServiceResult> DeleteAsync(Guid id);
}