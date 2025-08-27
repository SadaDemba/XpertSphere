using XpertSphere.MonolithApi.DTOs.TrainingDtos;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Interfaces;

/// <summary>
/// Service interface for educational training management operations
/// </summary>
public interface ITrainingService
{
    /// <summary>
    /// Get all educational trainings for a specific user
    /// </summary>
    Task<ServiceResult<IEnumerable<TrainingDto>>> GetUserTrainingsAsync(Guid userId);
    
    /// <summary>
    /// Get a specific educational training by ID
    /// </summary>
    Task<ServiceResult<TrainingDto>> GetTrainingByIdAsync(Guid id);
    
    /// <summary>
    /// Create a new educational training for a user
    /// </summary>
    /// <param name="createDto">The educational training data to create</param>
    /// <returns></returns>
    Task<ServiceResult<TrainingDto>> CreateTrainingAsync(CreateTrainingDto createDto);
    
    /// <summary>
    /// Update an existing educational training
    /// </summary>
    Task<ServiceResult<TrainingDto>> UpdateTrainingAsync(Guid id, UpdateTrainingDto updateDto);
    
    /// <summary>
    /// Delete a specific educational training
    /// </summary>
    Task<ServiceResult> DeleteTrainingAsync(Guid id);
    
    /// <summary>
    /// Delete all educational trainings for a specific user
    /// </summary>
    Task<ServiceResult> DeleteUserTrainingsAsync(Guid userId);
    
    /// <summary>
    /// Replace all educational trainings for a user with new ones (delete old, create new)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="trainings">List of new trainings to create</param>
    Task<ServiceResult<IEnumerable<TrainingDto>>> ReplaceUserTrainingsAsync(Guid userId, List<CreateTrainingDto> trainings);
    
    /// <summary>
    /// Assign an existing educational training to a different user
    /// </summary>
    Task<ServiceResult<TrainingDto>> AssignTrainingToUserAsync(Guid trainingId, Guid userId);
    
    /// <summary>
    /// Remove an educational training from a user (deletes the training)
    /// </summary>
    Task<ServiceResult> UnassignTrainingFromUserAsync(Guid trainingId);
    
    /// <summary>
    /// Check if a user has a specific educational training
    /// </summary>
    Task<ServiceResult<bool>> UserHasTrainingAsync(Guid userId, Guid trainingId);
    
    /// <summary>
    /// Check if an educational training can be deleted
    /// </summary>
    Task<ServiceResult<bool>> CanDeleteTrainingAsync(Guid id);
}