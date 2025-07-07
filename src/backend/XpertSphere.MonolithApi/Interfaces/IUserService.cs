using XpertSphere.MonolithApi.DTOs.User;

namespace XpertSphere.MonolithApi.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<(List<UserDto> Users, int TotalCount)> GetAllAsync(UserFilterDto filter);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateUserDto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null);
    Task<UserDto?> UpdateLastLoginAsync(Guid id);
    Task<List<UserDto>> GetByOrganizationAsync(Guid organizationId);
    Task<List<UserDto>> GetCandidatesAsync(UserFilterDto filter);
    Task<List<UserDto>> GetInternalUsersAsync(UserFilterDto filter);
}
