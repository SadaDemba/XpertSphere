using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get detailed user profile by ID
    /// </summary>
    Task<ServiceResult<UserProfileDto>> GetProfileAsync(Guid id);

    /// <summary>
    /// Search users with filters and pagination
    /// </summary>
    Task<PaginatedResult<UserSearchResultDto>> SearchAsync(UserFilterDto filter);

    /// <summary>
    /// Create a new user
    /// </summary>
    Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto);

    /// <summary>
    /// Update an existing user
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto);

    /// <summary>
    /// Soft delete a user (set IsActive = false)
    /// </summary>
    Task<ServiceResult> DeleteAsync(Guid id);

    /// <summary>
    /// Hard delete a user (remove from database)
    /// </summary>
    Task<ServiceResult> HardDeleteAsync(Guid id);

    /// <summary>
    /// Upload CV for a user
    /// </summary>
    Task<ServiceResult<UploadCvResponseDto>> UploadCvAsync(Guid userId, UploadCvDto dto);

    /// <summary>
    /// Get user statistics
    /// </summary>
    Task<ServiceResult<UserStatsDto>> GetStatsAsync(Guid id);

    /// <summary>
    /// Get users by organization
    /// </summary>
    Task<ServiceResult<List<UserSearchResultDto>>> GetByOrganizationAsync(Guid organizationId);

    /// <summary>
    /// Get dashboard statistics for a user
    /// </summary>
    Task<ServiceResult<UserDashboardStatsDto>> GetDashboardStatsAsync(Guid id);

    /// <summary>
    /// Activate a user account
    /// </summary>
    Task<ServiceResult> ActivateAsync(Guid id);

    /// <summary>
    /// Deactivate a user account
    /// </summary>
    Task<ServiceResult> DeactivateAsync(Guid id);

    /// <summary>
    /// Check if user exists
    /// </summary>
    Task<ServiceResult<bool>> ExistsAsync(Guid id);

    /// <summary>
    /// Check if email exists (optionally excluding a specific user)
    /// </summary>
    Task<ServiceResult<bool>> EmailExistsAsync(string email, Guid? excludeUserId = null);

    /// <summary>
    /// Update last login timestamp
    /// </summary>
    Task<ServiceResult> UpdateLastLoginAsync(Guid id);

    /// <summary>
    /// Calculate and update profile completion percentage
    /// </summary>
    Task<ServiceResult<int>> UpdateProfileCompletionAsync(Guid id);

    /// <summary>
    /// Get users with incomplete profiles (for admin purposes)
    /// </summary>
    Task<ServiceResult<List<UserSearchResultDto>>> GetUsersWithIncompleteProfilesAsync(int threshold = 80);

    /// <summary>
    /// Get recently registered users
    /// </summary>
    Task<ServiceResult<List<UserSearchResultDto>>> GetRecentlyRegisteredUsersAsync(int days = 7);

    /// <summary>
    /// Get inactive users (haven't logged in for X days)
    /// </summary>
    Task<ServiceResult<List<UserSearchResultDto>>> GetInactiveUsersAsync(int days = 30);

    /// <summary>
    /// Bulk update users (for admin operations)
    /// </summary>
    Task<ServiceResult<int>> BulkUpdateAsync(List<Guid> userIds, UpdateUserDto updates);
}
