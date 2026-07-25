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
            return ServiceResult<IEnumerable<TrainingDto>>.InternalError(
                "Une erreur est survenue lors de la récupération des formations de l'utilisateur");
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
                return ServiceResult<TrainingDto>.NotFound($"Formation avec l'ID {id} introuvable");
            }

            var trainingDto = _mapper.Map<TrainingDto>(training);
            return ServiceResult<TrainingDto>.Success(trainingDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training with ID {TrainingId}", id);
            return ServiceResult<TrainingDto>.InternalError("Une erreur est survenue lors de la récupération de la formation");
        }
    }

    public async Task<ServiceResult<TrainingDto>> CreateTrainingAsync(CreateTrainingDto createDto)
    {
        try
        {
            var training = _mapper.Map<Training>(createDto);
            await _context.Trainings.AddAsync(training);

            var trainingDto = _mapper.Map<TrainingDto>(createDto);
            return ServiceResult<TrainingDto>.Success(trainingDto, "Formation créée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training for user {UserId}", createDto.UserId);
            return ServiceResult<TrainingDto>.InternalError("Une erreur est survenue lors de la création de la formation");
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
                return ServiceResult<TrainingDto>.NotFound($"Formation avec l'ID {id} introuvable");
            }

            _mapper.Map(updateDto, training);
            training.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated training with ID {TrainingId}", id);

            var trainingDto = _mapper.Map<TrainingDto>(training);
            return ServiceResult<TrainingDto>.Success(trainingDto, "Formation mise à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training with ID {TrainingId}", id);
            return ServiceResult<TrainingDto>.InternalError("Une erreur est survenue lors de la mise à jour de la formation");
        }
    }

    public async Task<ServiceResult> DeleteTrainingAsync(Guid id)
    {
        try
        {
            var training = await _context.Trainings.FindAsync(id);
            if (training == null)
            {
                return ServiceResult.NotFound($"Formation avec l'ID {id} introuvable");
            }

            _context.Trainings.Remove(training);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted training with ID {TrainingId}", id);
            return ServiceResult.Success("Formation supprimée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training with ID {TrainingId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression de la formation");
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
                return ServiceResult.Success("Aucune formation à supprimer");
            }

            _context.Trainings.RemoveRange(trainings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} trainings for user {UserId}", trainings.Count, userId);
            return ServiceResult.Success($"{trainings.Count} formation(s) supprimée(s) avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trainings for user {UserId}", userId);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression des formations de l'utilisateur");
        }
    }

    public async Task<ServiceResult<IEnumerable<TrainingDto>>> ReplaceUserTrainingsAsync(Guid userId,
        List<CreateTrainingDto> trainings)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<IEnumerable<TrainingDto>>.NotFound($"Utilisateur avec l'ID {userId} introuvable");
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
                $"Formations de l'utilisateur remplacées avec succès par {trainings.Count} nouvelle(s) formation(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing trainings for user {UserId}", userId);
            return ServiceResult<IEnumerable<TrainingDto>>.InternalError(
                "Une erreur est survenue lors du remplacement des formations de l'utilisateur");
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
                return ServiceResult<TrainingDto>.NotFound($"Formation avec l'ID {trainingId} introuvable");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<TrainingDto>.NotFound($"Utilisateur avec l'ID {userId} introuvable");
            }

            if (training.UserId == userId)
            {
                return ServiceResult<TrainingDto>.Success(
                    _mapper.Map<TrainingDto>(training),
                    "La formation est déjà affectée à cet utilisateur");
            }

            training.UserId = userId;
            training.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned training {TrainingId} to user {UserId}", trainingId, userId);

            var trainingDto = _mapper.Map<TrainingDto>(training);
            return ServiceResult<TrainingDto>.Success(trainingDto, "Formation affectée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning training {TrainingId} to user {UserId}", trainingId, userId);
            return ServiceResult<TrainingDto>.InternalError("Une erreur est survenue lors de l'affectation de la formation");
        }
    }

    public async Task<ServiceResult> UnassignTrainingFromUserAsync(Guid trainingId)
    {
        try
        {
            var training = await _context.Trainings.FindAsync(trainingId);
            if (training == null)
            {
                return ServiceResult.NotFound($"Formation avec l'ID {trainingId} introuvable");
            }

            // Instead of unassigning, we delete the training
            _context.Trainings.Remove(training);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted training {TrainingId} (was assigned to user {UserId})",
                trainingId, training.UserId);
            return ServiceResult.Success("Formation supprimée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning training {TrainingId}", trainingId);
            return ServiceResult.InternalError("Une erreur est survenue lors de la désaffectation de la formation");
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
            return ServiceResult<bool>.InternalError("Une erreur est survenue lors de la vérification de la formation de l'utilisateur");
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
            return ServiceResult<bool>.InternalError("Une erreur est survenue lors de la vérification de la suppressibilité de la formation");
        }
    }
}