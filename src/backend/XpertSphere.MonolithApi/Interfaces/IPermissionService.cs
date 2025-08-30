using XpertSphere.MonolithApi.DTOs.Permission;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IPermissionService
{
    /// <summary>
    /// retrieve all permissions
    /// </summary>
    /// <returns></returns>
    Task<ServiceResult<IEnumerable<PermissionDto>>> GetAllPermissionsAsync();
    
    /// <summary>
    /// retrieve all paginated permissions
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task<PaginatedResult<PermissionDto>> GetAllPaginatedPermissionsAsync(PermissionFilterDto filter);
    
    /// <summary>
    /// Retrieve a single permission by its Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ServiceResult<PermissionDto>> GetPermissionByIdAsync(Guid id);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    Task<ServiceResult<IEnumerable<PermissionDto>>> GetPermissionsByResourceAsync(string resource);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    Task<ServiceResult<IEnumerable<PermissionDto>>> GetPermissionsByCategoryAsync(string category);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="createPermissionDto"></param>
    /// <returns></returns>
    Task<ServiceResult<PermissionDto>> CreatePermissionAsync(CreatePermissionDto createPermissionDto);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ServiceResult> DeletePermissionAsync(Guid id);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<ServiceResult<bool>> PermissionExistsAsync(string name);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ServiceResult<bool>> CanDeletePermissionAsync(Guid id);
}