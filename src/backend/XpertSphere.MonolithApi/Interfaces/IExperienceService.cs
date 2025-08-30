using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

/// <summary>
/// Service interface for professional experience management operations
/// </summary>
public interface IExperienceService
{
    /// <summary>
    /// Get all professional experiences for a specific user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<ServiceResult<IEnumerable<ExperienceDto>>> GetUserExperiencesAsync(Guid userId);
    
    /// <summary>
    /// Get a specific professional experience by ID
    /// </summary>
    Task<ServiceResult<ExperienceDto>> GetExperienceByIdAsync(Guid id);
    
    /// <summary>
    /// Create a new professional experience for a user
    /// </summary>
    /// <param name="createDto">The professional experience data to create</param>
    /// <returns></returns>
    Task<ServiceResult<ExperienceDto>> CreateExperienceAsync(CreateExperienceDto createDto);

    /// <summary>
    /// Update an existing professional experience
    /// </summary>
    Task<ServiceResult<ExperienceDto>> UpdateExperienceAsync(Guid id, UpdateExperienceDto updateDto);

    /// <summary>
    /// Delete a specific professional experience
    /// </summary>
    Task<ServiceResult> DeleteExperienceAsync(Guid id);
    
    /// <summary>
    /// Delete all professional experiences for a specific user
    /// </summary>
    Task<ServiceResult> DeleteUserExperiencesAsync(Guid userId);

    /// <summary>
    /// Replace all professional experiences for a user with new ones (delete old, create new)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="experiences">List of new experiences to create</param>
    Task<ServiceResult<IEnumerable<ExperienceDto>>> ReplaceUserExperiencesAsync(Guid userId, List<CreateExperienceDto> experiences);

    /// <summary>
    /// Assign an existing professional experience to a different user
    /// </summary>
    Task<ServiceResult<ExperienceDto>> AssignExperienceToUserAsync(Guid experienceId, Guid userId);
    
    /// <summary>
    /// Remove a professional experience from a user (deletes the experience)
    /// </summary>
    Task<ServiceResult> UnassignExperienceFromUserAsync(Guid experienceId);

    /// <summary>
    /// Check if a user has a specific professional experience
    /// </summary>
    Task<ServiceResult<bool>> UserHasExperienceAsync(Guid userId, Guid experienceId);
    
    /// <summary>
    /// Check if a professional experience can be deleted
    /// </summary>
    Task<ServiceResult<bool>> CanDeleteExperienceAsync(Guid id);
}