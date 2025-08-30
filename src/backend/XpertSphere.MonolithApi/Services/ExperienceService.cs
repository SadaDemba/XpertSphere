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
            return ServiceResult<IEnumerable<ExperienceDto>>.InternalError("An error occurred while retrieving user experiences");
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
                return ServiceResult<ExperienceDto>.NotFound($"Experience with ID {id} not found");
            }

            var experienceDto = _mapper.Map<ExperienceDto>(experience);
            return ServiceResult<ExperienceDto>.Success(experienceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving experience with ID {ExperienceId}", id);
            return ServiceResult<ExperienceDto>.InternalError("An error occurred while retrieving the experience");
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
            return ServiceResult<ExperienceDto>.Success(experienceDto, "Experience created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating experience for user {UserId}", createDto.UserId);
            return ServiceResult<ExperienceDto>.InternalError("An error occurred while creating the experience");
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
                return ServiceResult<ExperienceDto>.NotFound($"Experience with ID {id} not found");
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
            return ServiceResult<ExperienceDto>.Success(experienceDto, "Experience updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating experience with ID {ExperienceId}", id);
            return ServiceResult<ExperienceDto>.InternalError("An error occurred while updating the experience");
        }
    }

    public async Task<ServiceResult> DeleteExperienceAsync(Guid id)
    {
        try
        {
            var experience = await _context.Experiences.FindAsync(id);
            if (experience == null)
            {
                return ServiceResult.NotFound($"Experience with ID {id} not found");
            }

            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted experience with ID {ExperienceId}", id);
            return ServiceResult.Success("Experience deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting experience with ID {ExperienceId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the experience");
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
                return ServiceResult.Success("No experiences to delete");
            }

            _context.Experiences.RemoveRange(experiences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} experiences for user {UserId}", experiences.Count, userId);
            return ServiceResult.Success($"Deleted {experiences.Count} experiences successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting experiences for user {UserId}", userId);
            return ServiceResult.InternalError("An error occurred while deleting user experiences");
        }
    }

    public async Task<ServiceResult<IEnumerable<ExperienceDto>>> ReplaceUserExperiencesAsync(Guid userId, List<CreateExperienceDto> experiences)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<IEnumerable<ExperienceDto>>.NotFound($"User with ID {userId} not found");
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
                $"Successfully replaced user experiences with {experiences.Count} new experiences");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing experiences for user {UserId}", userId);
            return ServiceResult<IEnumerable<ExperienceDto>>.InternalError("An error occurred while replacing user experiences");
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
                return ServiceResult<ExperienceDto>.NotFound($"Experience with ID {experienceId} not found");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return ServiceResult<ExperienceDto>.NotFound($"User with ID {userId} not found");
            }

            if (experience.UserId == userId)
            {
                return ServiceResult<ExperienceDto>.Success(
                    _mapper.Map<ExperienceDto>(experience),
                    "Experience is already assigned to this user");
            }

            experience.UserId = userId;
            experience.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned experience {ExperienceId} to user {UserId}", experienceId, userId);

            var experienceDto = _mapper.Map<ExperienceDto>(experience);
            return ServiceResult<ExperienceDto>.Success(experienceDto, "Experience assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning experience {ExperienceId} to user {UserId}", experienceId, userId);
            return ServiceResult<ExperienceDto>.InternalError("An error occurred while assigning the experience");
        }
    }

    public async Task<ServiceResult> UnassignExperienceFromUserAsync(Guid experienceId)
    {
        try
        {
            var experience = await _context.Experiences.FindAsync(experienceId);
            if (experience == null)
            {
                return ServiceResult.NotFound($"Experience with ID {experienceId} not found");
            }

            // Instead of unassigning, we delete the experience
            _context.Experiences.Remove(experience);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted experience {ExperienceId} (was assigned to user {UserId})", 
                experienceId, experience.UserId);
            return ServiceResult.Success("Experience removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning experience {ExperienceId}", experienceId);
            return ServiceResult.InternalError("An error occurred while unassigning the experience");
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
            return ServiceResult<bool>.InternalError("An error occurred while checking user experience");
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
            return ServiceResult<bool>.InternalError("An error occurred while checking if experience can be deleted");
        }
    }
}