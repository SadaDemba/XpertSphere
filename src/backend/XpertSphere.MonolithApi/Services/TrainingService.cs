using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.TrainingDtos;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class TrainingService : ITrainingService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TrainingService> _logger;
    private readonly IValidator<UpdateTrainingDto>? _updateValidator;

    public TrainingService(
        XpertSphereDbContext context,
        IMapper mapper,
        ILogger<TrainingService> logger,
        IValidator<UpdateTrainingDto>? updateValidator = null)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _updateValidator = updateValidator;
    }
    
    public async Task<ServiceResult<IEnumerable<TrainingDto>>> GetUserTrainingsAsync(Guid userId)
    {
        try
        {
            var trainings = await _context.Trainings
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Period)
                .ToListAsync();

            var trainingDtos = _mapper.Map<IEnumerable<TrainingDto>>(trainings);
            return ServiceResult<IEnumerable<TrainingDto>>.Success(trainingDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trainings for user {UserId}", userId);
            return ServiceResult<IEnumerable<TrainingDto>>.InternalError("An error occurred while retrieving user trainings");
        }
    }

    public async Task<ServiceResult<TrainingDto>> GetTrainingByIdAsync(Guid id)
    {
        try
        {
            var training = await _context.Trainings
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training == null)
            {
                return ServiceResult<TrainingDto>.NotFound($"Training with ID {id} not found");
            }

            var trainingDto = _mapper.Map<TrainingDto>(training);
            return ServiceResult<TrainingDto>.Success(trainingDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training with ID {TrainingId}", id);
            return ServiceResult<TrainingDto>.InternalError("An error occurred while retrieving the training");
        }
    }

    public async Task<ServiceResult<TrainingDto>> CreateTrainingAsync(CreateTrainingDto createDto)
    {
        try
        {
            var training = _mapper.Map<Training>(createDto);
            await _context.Trainings.AddAsync(training);
            
            var trainingDto = _mapper.Map<TrainingDto>(createDto);
            return ServiceResult<TrainingDto>.Success(trainingDto, "Training created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training for user {UserId}", createDto.UserId);
            return ServiceResult<TrainingDto>.InternalError("An error occurred while creating the training");
        }
    }

    public async Task<ServiceResult<TrainingDto>> UpdateTrainingAsync(Guid id, UpdateTrainingDto updateDto)
    {
        try
        {
            // Validate if validator is available
            if (_updateValidator != null)
            {
                var validationResult = await _updateValidator.ValidateAsync(updateDto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return ServiceResult<TrainingDto>.ValidationError(errors);
                }
            }

            var training = await _context.Trainings
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training == null)
            {
                return ServiceResult<TrainingDto>.NotFound($"Training with ID {id} not found");
            }

            _mapper.Map(updateDto, training);
            training.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated training with ID {TrainingId}", id);

            var trainingDto = _mapper.Map<TrainingDto>(training);
            return ServiceResult<TrainingDto>.Success(trainingDto, "Training updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training with ID {TrainingId}", id);
            return ServiceResult<TrainingDto>.InternalError("An error occurred while updating the training");
        }
    }

    public async Task<ServiceResult> DeleteTrainingAsync(Guid id)
    {
        try
        {
            var training = await _context.Trainings.FindAsync(id);
            if (training == null)
            {
                return ServiceResult.NotFound($"Training with ID {id} not found");
            }

            _context.Trainings.Remove(training);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted training with ID {TrainingId}", id);
            return ServiceResult.Success("Training deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training with ID {TrainingId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the training");
        }
    }

    public async Task<ServiceResult> DeleteUserTrainingsAsync(Guid userId)
    {
        try
        {
            var trainings = await _context.Trainings
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (!trainings.Any())
            {
                return ServiceResult.Success("No trainings to delete");
            }

            _context.Trainings.RemoveRange(trainings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} trainings for user {UserId}", trainings.Count, userId);
            return ServiceResult.Success($"Deleted {trainings.Count} trainings successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trainings for user {UserId}", userId);
            return ServiceResult.InternalError("An error occurred while deleting user trainings");
        }
    }

    public async Task<ServiceResult<IEnumerable<TrainingDto>>> ReplaceUserTrainingsAsync(Guid userId, List<CreateTrainingDto> trainings)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<IEnumerable<TrainingDto>>.NotFound($"User with ID {userId} not found");
            }

            // Delete all existing trainings for the user
            var existingTrainings = await _context.Trainings
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (existingTrainings.Any())
            {
                _context.Trainings.RemoveRange(existingTrainings);
            }

            // Create new trainings
            var newTrainings = new List<Training>();
            foreach (var trainingDto in trainings)
            {
                // Set the userId for each training
                trainingDto.UserId = userId;
                
                var training = _mapper.Map<Training>(trainingDto);
                training.Id = Guid.NewGuid();
                training.CreatedAt = DateTime.UtcNow;
                
                _context.Trainings.Add(training);
                newTrainings.Add(training);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Replaced {OldCount} trainings with {NewCount} trainings for user {UserId}", 
                existingTrainings.Count, trainings.Count, userId);

            // Reload with User info for DTOs
            var savedTrainings = await _context.Trainings
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Period)
                .ToListAsync();

            var trainingDtos = _mapper.Map<IEnumerable<TrainingDto>>(savedTrainings);
            return ServiceResult<IEnumerable<TrainingDto>>.Success(trainingDtos, 
                $"Successfully replaced user trainings with {trainings.Count} new trainings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing trainings for user {UserId}", userId);
            return ServiceResult<IEnumerable<TrainingDto>>.InternalError("An error occurred while replacing user trainings");
        }
    }

    public async Task<ServiceResult<TrainingDto>> AssignTrainingToUserAsync(Guid trainingId, Guid userId)
    {
        try
        {
            var training = await _context.Trainings
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == trainingId);

            if (training == null)
            {
                return ServiceResult<TrainingDto>.NotFound($"Training with ID {trainingId} not found");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<TrainingDto>.NotFound($"User with ID {userId} not found");
            }

            if (training.UserId == userId)
            {
                return ServiceResult<TrainingDto>.Success(
                    _mapper.Map<TrainingDto>(training),
                    "Training is already assigned to this user");
            }

            training.UserId = userId;
            training.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned training {TrainingId} to user {UserId}", trainingId, userId);

            var trainingDto = _mapper.Map<TrainingDto>(training);
            return ServiceResult<TrainingDto>.Success(trainingDto, "Training assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning training {TrainingId} to user {UserId}", trainingId, userId);
            return ServiceResult<TrainingDto>.InternalError("An error occurred while assigning the training");
        }
    }

    public async Task<ServiceResult> UnassignTrainingFromUserAsync(Guid trainingId)
    {
        try
        {
            var training = await _context.Trainings.FindAsync(trainingId);
            if (training == null)
            {
                return ServiceResult.NotFound($"Training with ID {trainingId} not found");
            }

            // Instead of unassigning, we delete the training
            _context.Trainings.Remove(training);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted training {TrainingId} (was assigned to user {UserId})", 
                trainingId, training.UserId);
            return ServiceResult.Success("Training removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning training {TrainingId}", trainingId);
            return ServiceResult.InternalError("An error occurred while unassigning the training");
        }
    }

    public async Task<ServiceResult<bool>> UserHasTrainingAsync(Guid userId, Guid trainingId)
    {
        try
        {
            var hasTraining = await _context.Trainings
                .AnyAsync(t => t.Id == trainingId && t.UserId == userId);
            
            return ServiceResult<bool>.Success(hasTraining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has training {TrainingId}", userId, trainingId);
            return ServiceResult<bool>.InternalError("An error occurred while checking user training");
        }
    }

    public async Task<ServiceResult<bool>> CanDeleteTrainingAsync(Guid id)
    {
        try
        {
            var exists = await _context.Trainings.AnyAsync(t => t.Id == id);
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if training {TrainingId} can be deleted", id);
            return ServiceResult<bool>.InternalError("An error occurred while checking if training can be deleted");
        }
    }
}