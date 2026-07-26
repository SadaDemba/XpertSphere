using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class ExperienceService : IExperienceService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ExperienceService> _logger;
    private readonly IValidator<CreateExperienceDto>? _createValidator;
    private readonly IValidator<UpdateExperienceDto>? _updateValidator;

    public ExperienceService(
        XpertSphereDbContext context,
        IMapper mapper,
        ILogger<ExperienceService> logger,
        IValidator<CreateExperienceDto>? createValidator = null,
        IValidator<UpdateExperienceDto>? updateValidator = null)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ServiceResult<IEnumerable<ExperienceDto>>> GetUserExperiencesAsync(Guid userId)
    {
        try
        {
            var experiences = await _context.Experiences
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.IsCurrent)
                .ThenByDescending(e => e.Date)
                .ToListAsync();

            var experienceDtos = _mapper.Map<IEnumerable<ExperienceDto>>(experiences);
            return ServiceResult<IEnumerable<ExperienceDto>>.Success(experienceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving experiences for user {UserId}", userId);
            return ServiceResult<IEnumerable<ExperienceDto>>.InternalError(
                "Une erreur est survenue lors de la récupération des expériences de l'utilisateur");
        }
    }

    public async Task<ServiceResult<ExperienceDto>> GetExperienceByIdAsync(Guid id)
    {
        try
        {
            var experience = await _context.Experiences
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (experience == null)
            {
                return ServiceResult<ExperienceDto>.NotFound($"Expérience avec l'ID {id} introuvable");
            }

            var experienceDto = _mapper.Map<ExperienceDto>(experience);
            return ServiceResult<ExperienceDto>.Success(experienceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving experience with ID {ExperienceId}", id);
            return ServiceResult<ExperienceDto>.InternalError("Une erreur est survenue lors de la récupération de l'expérience");
        }
    }

    public async Task<ServiceResult<ExperienceDto>> CreateExperienceAsync(CreateExperienceDto createDto)
    {
        try
        {
            var experience = _mapper.Map<Experience>(createDto);
            // If IsCurrent is true, update other experiences for this user
            if (createDto.IsCurrent)
            {
                var currentExperiences = await _context.Experiences
                    .Where(e => e.UserId == createDto.UserId && e.IsCurrent)
                    .ToListAsync();

                foreach (var exp in currentExperiences)
                {
                    exp.IsCurrent = false;
                }
            }

            await _context.Experiences.AddAsync(experience);

            var experienceDto = _mapper.Map<ExperienceDto>(createDto);
            return ServiceResult<ExperienceDto>.Success(experienceDto, "Expérience créée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating experience for user {UserId}", createDto.UserId);
            return ServiceResult<ExperienceDto>.InternalError("Une erreur est survenue lors de la création de l'expérience");
        }
    }

    public async Task<ServiceResult<ExperienceDto>> UpdateExperienceAsync(Guid id, UpdateExperienceDto updateDto)
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
                    return ServiceResult<ExperienceDto>.ValidationError(errors);
                }
            }

            var experience = await _context.Experiences
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (experience == null)
            {
                return ServiceResult<ExperienceDto>.NotFound($"Expérience avec l'ID {id} introuvable");
            }

            // If updating to IsCurrent = true, update other experiences
            if (updateDto.IsCurrent == true && !experience.IsCurrent)
            {
                var currentExperiences = await _context.Experiences
                    .Where(e => e.UserId == experience.UserId && e.IsCurrent && e.Id != id)
                    .ToListAsync();

                foreach (var exp in currentExperiences)
                {
                    exp.IsCurrent = false;
                }
            }

            _mapper.Map(updateDto, experience);
            experience.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated experience with ID {ExperienceId}", id);

            var experienceDto = _mapper.Map<ExperienceDto>(experience);
            return ServiceResult<ExperienceDto>.Success(experienceDto, "Expérience mise à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating experience with ID {ExperienceId}", id);
            return ServiceResult<ExperienceDto>.InternalError("Une erreur est survenue lors de la mise à jour de l'expérience");
        }
    }

    public async Task<ServiceResult> DeleteExperienceAsync(Guid id)
    {
        try
        {
            var experience = await _context.Experiences.FindAsync(id);
            if (experience == null)
            {
                return ServiceResult.NotFound($"Expérience avec l'ID {id} introuvable");
            }

            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted experience with ID {ExperienceId}", id);
            return ServiceResult.Success("Expérience supprimée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting experience with ID {ExperienceId}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression de l'expérience");
        }
    }

    public async Task<ServiceResult> DeleteUserExperiencesAsync(Guid userId)
    {
        try
        {
            var experiences = await _context.Experiences
                .Where(e => e.UserId == userId)
                .ToListAsync();

            if (!experiences.Any())
            {
                return ServiceResult.Success("Aucune expérience à supprimer");
            }

            _context.Experiences.RemoveRange(experiences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} experiences for user {UserId}", experiences.Count, userId);
            return ServiceResult.Success($"{experiences.Count} expérience(s) supprimée(s) avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting experiences for user {UserId}", userId);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression des expériences de l'utilisateur");
        }
    }

    public async Task<ServiceResult<IEnumerable<ExperienceDto>>> ReplaceUserExperiencesAsync(Guid userId,
        List<CreateExperienceDto> experiences)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<IEnumerable<ExperienceDto>>.NotFound($"Utilisateur avec l'ID {userId} introuvable");
            }

            // Delete all existing experiences for the user
            var existingExperiences = await _context.Experiences
                .Where(e => e.UserId == userId)
                .ToListAsync();

            if (existingExperiences.Count != 0)
            {
                _context.Experiences.RemoveRange(existingExperiences);
            }

            // Create new experiences
            var newExperiences = new List<Experience>();
            foreach (var experienceDto in experiences)
            {
                // Set the userId for each experience
                experienceDto.UserId = userId;

                var experience = _mapper.Map<Experience>(experienceDto);

                _context.Experiences.Add(experience);
                newExperiences.Add(experience);
            }

            // Handle IsCurrent logic - only one experience can be current
            var currentExperiences = newExperiences.Where(e => e.IsCurrent).ToList();
            if (currentExperiences.Count > 1)
            {
                // Keep only the last one as current
                for (int i = 0; i < currentExperiences.Count - 1; i++)
                {
                    currentExperiences[i].IsCurrent = false;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Replaced {OldCount} experiences with {NewCount} experiences for user {UserId}",
                existingExperiences.Count, experiences.Count, userId);

            // Reload with User info for DTOs
            var savedExperiences = await _context.Experiences
                .Include(e => e.User)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.IsCurrent)
                .ThenByDescending(e => e.Date)
                .ToListAsync();

            var experienceDtos = _mapper.Map<IEnumerable<ExperienceDto>>(savedExperiences);
            return ServiceResult<IEnumerable<ExperienceDto>>.Success(experienceDtos,
                $"Expériences de l'utilisateur remplacées avec succès par {experiences.Count} nouvelle(s) expérience(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing experiences for user {UserId}", userId);
            return ServiceResult<IEnumerable<ExperienceDto>>.InternalError(
                "Une erreur est survenue lors du remplacement des expériences de l'utilisateur");
        }
    }

    public async Task<ServiceResult<ExperienceDto>> AssignExperienceToUserAsync(Guid experienceId, Guid userId)
    {
        try
        {
            var experience = await _context.Experiences
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == experienceId);

            if (experience == null)
            {
                return ServiceResult<ExperienceDto>.NotFound($"Expérience avec l'ID {experienceId} introuvable");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<ExperienceDto>.NotFound($"Utilisateur avec l'ID {userId} introuvable");
            }

            if (experience.UserId == userId)
            {
                return ServiceResult<ExperienceDto>.Success(
                    _mapper.Map<ExperienceDto>(experience),
                    "L'expérience est déjà affectée à cet utilisateur");
            }

            experience.UserId = userId;
            experience.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned experience {ExperienceId} to user {UserId}", experienceId, userId);

            var experienceDto = _mapper.Map<ExperienceDto>(experience);
            return ServiceResult<ExperienceDto>.Success(experienceDto, "Expérience affectée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning experience {ExperienceId} to user {UserId}", experienceId, userId);
            return ServiceResult<ExperienceDto>.InternalError("Une erreur est survenue lors de l'affectation de l'expérience");
        }
    }

    public async Task<ServiceResult> UnassignExperienceFromUserAsync(Guid experienceId)
    {
        try
        {
            var experience = await _context.Experiences.FindAsync(experienceId);
            if (experience == null)
            {
                return ServiceResult.NotFound($"Expérience avec l'ID {experienceId} introuvable");
            }

            // Instead of unassigning, we delete the experience
            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted experience {ExperienceId} (was assigned to user {UserId})",
                experienceId, experience.UserId);
            return ServiceResult.Success("Expérience supprimée avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning experience {ExperienceId}", experienceId);
            return ServiceResult.InternalError("Une erreur est survenue lors de la désaffectation de l'expérience");
        }
    }

    public async Task<ServiceResult<bool>> UserHasExperienceAsync(Guid userId, Guid experienceId)
    {
        try
        {
            var hasExperience = await _context.Experiences
                .AnyAsync(e => e.Id == experienceId && e.UserId == userId);

            return ServiceResult<bool>.Success(hasExperience);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has experience {ExperienceId}", userId, experienceId);
            return ServiceResult<bool>.InternalError("Une erreur est survenue lors de la vérification de l'expérience de l'utilisateur");
        }
    }

    public async Task<ServiceResult<bool>> CanDeleteExperienceAsync(Guid id)
    {
        try
        {
            var exists = await _context.Experiences.AnyAsync(e => e.Id == id);
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if experience {ExperienceId} can be deleted", id);
            return ServiceResult<bool>.InternalError("Une erreur est survenue lors de la vérification de la suppressibilité de l'expérience");
        }
    }
}