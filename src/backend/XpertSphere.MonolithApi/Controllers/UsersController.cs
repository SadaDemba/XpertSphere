using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Extensions;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    #region Basic CRUD Operations

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    /// <param name="filter">Filter criteria for searching users</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [Authorize(Policy = "OrganizationIsolation")]
    [ProducesResponseType(typeof(PaginatedResult<UserSearchResultDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<PaginatedResult<UserSearchResultDto>>> GetUsers([FromQuery] UserFilterDto filter)
    {
        var result = await _userService.SearchAsync(filter);
        return this.ToPaginatedActionResult(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CandidateOwnDataAccess")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get detailed user profile by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Detailed user profile</returns>
    [HttpGet("{id:guid}/profile")]
    [Authorize(Policy = "CandidateOwnDataAccess")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserProfileDto>> GetUserProfile(Guid id)
    {
        var result = await _userService.GetProfileAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="createUserDto">User creation data</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [Authorize(Policy = "CanCreateUsers")]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        var result = await _userService.CreateAsync(createUserDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserDto">User update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CandidateOwnDataAccess")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
    {
        var result = await _userService.UpdateAsync(id, updateUserDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Soft delete a user (deactivate)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanCreateUsers")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Permanently delete a user from the database
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error result</returns>
    [HttpDelete("{id:guid}/permanent")]
    [Authorize(Policy = "RequirePlatformSuperAdminRole")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> HardDeleteUser(Guid id)
    {
        var result = await _userService.HardDeleteAsync(id);
        return this.ToActionResult(result);
    }

    #endregion

    #region User Management Actions

    /// <summary>
    /// Activate a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error result</returns>
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> ActivateUser(Guid id)
    {
        var result = await _userService.ActivateAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Deactivate a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error result</returns>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> DeactivateUser(Guid id)
    {
        var result = await _userService.DeactivateAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update user's last login timestamp
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success or error result</returns>
    [HttpPatch("{id:guid}/last-login")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> UpdateLastLogin(Guid id)
    {
        var result = await _userService.UpdateLastLoginAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Recalculate and update profile completion percentage
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Updated profile completion percentage</returns>
    [HttpPatch("{id:guid}/profile-completion")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<int>> UpdateProfileCompletion(Guid id)
    {
        var result = await _userService.UpdateProfileCompletionAsync(id);
        return this.ToActionResult(result);
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Upload CV for a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="uploadCvDto">CV upload data</param>
    /// <returns>Upload result with file details</returns>
    [HttpPost("{id:guid}/cv")]
    [ProducesResponseType(typeof(UploadCvResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UploadCvResponseDto>> UploadCv(Guid id, [FromForm] UploadCvDto uploadCvDto)
    {
        var result = await _userService.UploadCvAsync(id, uploadCvDto);
        return this.ToActionResult(result);
    }

    #endregion

    #region Utility & Validation Endpoints

    /// <summary>
    /// Check if a user exists
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>HTTP 200 if exists, 404 if not</returns>
    [HttpHead("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<bool>> UserExists(Guid id)
    {
        var result = await _userService.ExistsAsync(id);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Check if an email is available (not already in use)
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <param name="excludeUserId">Optional user ID to exclude from check</param>
    /// <returns>Availability status</returns>
    [HttpGet("email-available")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<bool>> EmailAvailable([FromQuery] string email,
        [FromQuery] Guid? excludeUserId = null)
    {
        var result = await _userService.EmailExistsAsync(email, excludeUserId);
        return this.ToActionResult(result);
    }

    #endregion

    #region Organization & Advanced Search

    /// <summary>
    /// Get all users belonging to a specific organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>List of users in the organization</returns>
    [HttpGet("organization/{organizationId:guid}")]
    [ProducesResponseType(typeof(List<UserSearchResultDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<UserSearchResultDto>>> GetUsersByOrganization(Guid organizationId)
    {
        var result = await _userService.GetByOrganizationAsync(organizationId);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get users with incomplete profiles (for admin/HR purposes)
    /// </summary>
    /// <param name="threshold">Profile completion threshold (default: 80%)</param>
    /// <returns>List of users with incomplete profiles</returns>
    [HttpGet("incomplete-profiles")]
    [ProducesResponseType(typeof(List<UserSearchResultDto>), 200)]
    public async Task<ActionResult<List<UserSearchResultDto>>> GetUsersWithIncompleteProfiles(
        [FromQuery] int threshold = 80)
    {
        var result = await _userService.GetUsersWithIncompleteProfilesAsync(threshold);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get recently registered users
    /// </summary>
    /// <param name="days">Number of days to look back (default: 7)</param>
    /// <returns>List of recently registered users</returns>
    [HttpGet("recently-registered")]
    [ProducesResponseType(typeof(List<UserSearchResultDto>), 200)]
    public async Task<ActionResult<List<UserSearchResultDto>>> GetRecentlyRegisteredUsers([FromQuery] int days = 7)
    {
        var result = await _userService.GetRecentlyRegisteredUsersAsync(days);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get inactive users (haven't logged in for specified days)
    /// </summary>
    /// <param name="days">Number of days to consider for inactivity (default: 30)</param>
    /// <returns>List of inactive users</returns>
    [HttpGet("inactive")]
    [ProducesResponseType(typeof(List<UserSearchResultDto>), 200)]
    public async Task<ActionResult<List<UserSearchResultDto>>> GetInactiveUsers([FromQuery] int days = 30)
    {
        var result = await _userService.GetInactiveUsersAsync(days);
        return this.ToActionResult(result);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bulk update multiple users (admin operation)
    /// </summary>
    /// <param name="bulkUpdateDto">Bulk update data with user IDs and updates</param>
    /// <returns>Number of successfully updated users</returns>
    [HttpPatch("bulk-update")]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<int>> BulkUpdateUsers([FromBody] BulkUpdateUsersDto bulkUpdateDto)
    {
        var result = await _userService.BulkUpdateAsync(bulkUpdateDto.UserIds, bulkUpdateDto.Updates);
        return this.ToActionResult(result);
    }

    #endregion

    #region Profile Updates

    /// <summary>
    /// Update user skills (replace all existing skills)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateSkillsDto">New skills data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}/skills")]
    [Authorize(Policy = "CandidateOwnDataAccess")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> UpdateUserSkills(Guid id, [FromBody] UpdateUserSkillsDto updateSkillsDto)
    {
        var result = await _userService.UpdateSkillsAsync(id, updateSkillsDto);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update user profile general information
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateProfileDto">Profile update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id:guid}/profile")]
    [Authorize(Policy = "CandidateOwnDataAccess")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDto>> UpdateUserProfile(Guid id, [FromBody] UpdateUserProfileDto updateProfileDto)
    {
        var result = await _userService.UpdateProfileAsync(id, updateProfileDto);
        return this.ToActionResult(result);
    }

    #endregion
}