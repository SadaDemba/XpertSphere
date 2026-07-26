using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class ApplicationStatusHistoryService : IApplicationStatusHistoryService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<AddStatusChangeDto> _addStatusChangeValidator;
    private readonly IValidator<CreateApplicationStatusHistoryDto> _createValidator;
    private readonly IValidator<UpdateApplicationStatusHistoryDto> _updateValidator;
    private readonly ILogger<ApplicationStatusHistoryService> _logger;

    public ApplicationStatusHistoryService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<AddStatusChangeDto> addStatusChangeValidator,
        IValidator<CreateApplicationStatusHistoryDto> createValidator,
        IValidator<UpdateApplicationStatusHistoryDto> updateValidator,
        ILogger<ApplicationStatusHistoryService> logger)
    {
        _context = context;
        _mapper = mapper;
        _addStatusChangeValidator = addStatusChangeValidator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<ApplicationStatusHistoryDto>> AddStatusChangeAsync(
        AddStatusChangeDto addStatusChangeDto)
    {
        try
        {
            var validationResult = await _addStatusChangeValidator.ValidateAsync(addStatusChangeDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationStatusHistoryDto>.ValidationError(errors);
            }

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == addStatusChangeDto.ApplicationId);

            if (application == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound(
                    $"Candidature avec l'ID {addStatusChangeDto.ApplicationId} introuvable");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == addStatusChangeDto.UpdatedByUserId);

            if (user == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound(
                    $"Utilisateur avec l'ID {addStatusChangeDto.UpdatedByUserId} introuvable");
            }

            var statusHistory = _mapper.Map<ApplicationStatusHistory>(addStatusChangeDto);
            statusHistory.Id = Guid.NewGuid();
            statusHistory.UpdatedAt = DateTime.UtcNow;

            _context.ApplicationStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Added status change for application {ApplicationId}. Status: {Status}, UpdatedBy: {UserId}",
                addStatusChangeDto.ApplicationId,
                addStatusChangeDto.Status,
                addStatusChangeDto.UpdatedByUserId);

            return ServiceResult<ApplicationStatusHistoryDto>.Success(
                _mapper.Map<ApplicationStatusHistoryDto>(statusHistory), "Changement de statut ajouté avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error adding status change for application {ApplicationId}",
                addStatusChangeDto.ApplicationId);
            return ServiceResult<ApplicationStatusHistoryDto>.InternalError(
                "Une erreur est survenue lors de l'ajout du changement de statut");
        }
    }


    public async Task<ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>> GetByApplicationIdAsync(
        Guid applicationId)
    {
        try
        {
            var history = await _context.ApplicationStatusHistories
                .Include(h => h.UpdatedByUser)
                .Where(h => h.ApplicationId == applicationId)
                .OrderByDescending(h => h.UpdatedAt)
                .ToListAsync();

            var historyDtos = _mapper.Map<IEnumerable<ApplicationStatusHistoryDto>>(history);

            _logger.LogInformation("Retrieved {Count} status history entries for application {ApplicationId}",
                history.Count, applicationId);

            return ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>.Success(historyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status history for application {ApplicationId}", applicationId);
            return ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>.InternalError(
                "Une erreur est survenue lors de la récupération de l'historique des statuts");
        }
    }

    public async Task<ServiceResult<ApplicationStatusHistoryDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var statusHistory = await _context.ApplicationStatusHistories
                .Include(h => h.UpdatedByUser)
                .Include(h => h.Application)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (statusHistory == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound($"Historique de statut avec l'ID {id} introuvable");
            }

            var statusHistoryDto = _mapper.Map<ApplicationStatusHistoryDto>(statusHistory);
            return ServiceResult<ApplicationStatusHistoryDto>.Success(statusHistoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status history with ID {Id}", id);
            return ServiceResult<ApplicationStatusHistoryDto>.InternalError(
                "Une erreur est survenue lors de la récupération de l'historique des statuts");
        }
    }

    public async Task<ServiceResult<ApplicationStatusHistoryDto>> CreateAsync(CreateApplicationStatusHistoryDto dto,
        Guid updatedByUserId)
    {
        try
        {
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationStatusHistoryDto>.ValidationError(errors);
            }

            var application = await _context.Applications.FindAsync(dto.ApplicationId);
            if (application == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound(
                    $"Candidature avec l'ID {dto.ApplicationId} introuvable");
            }

            var user = await _context.Users.FindAsync(updatedByUserId);
            if (user == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound($"Utilisateur avec l'ID {updatedByUserId} introuvable");
            }

            var statusHistory = _mapper.Map<ApplicationStatusHistory>(dto);
            statusHistory.Id = Guid.NewGuid();
            statusHistory.UpdatedByUserId = updatedByUserId;
            statusHistory.UpdatedAt = DateTime.UtcNow;

            _context.ApplicationStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            var createdStatusHistory = await _context.ApplicationStatusHistories
                .Include(h => h.UpdatedByUser)
                .Include(h => h.Application)
                .FirstAsync(h => h.Id == statusHistory.Id);

            var statusHistoryDto = _mapper.Map<ApplicationStatusHistoryDto>(createdStatusHistory);

            _logger.LogInformation("Created status history with ID {Id} for application {ApplicationId}",
                statusHistory.Id, dto.ApplicationId);

            return ServiceResult<ApplicationStatusHistoryDto>.Success(statusHistoryDto,
                "Historique de statut créé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating status history for application {ApplicationId}", dto.ApplicationId);
            return ServiceResult<ApplicationStatusHistoryDto>.InternalError(
                "Une erreur est survenue lors de la création de l'historique de statut");
        }
    }

    public async Task<ServiceResult<ApplicationStatusHistoryDto>> UpdateAsync(Guid id,
        UpdateApplicationStatusHistoryDto dto, Guid updatedByUserId)
    {
        try
        {
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationStatusHistoryDto>.ValidationError(errors);
            }

            var statusHistory = await _context.ApplicationStatusHistories
                .Include(h => h.UpdatedByUser)
                .Include(h => h.Application)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (statusHistory == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound($"Historique de statut avec l'ID {id} introuvable");
            }

            _mapper.Map(dto, statusHistory);
            statusHistory.UpdatedByUserId = updatedByUserId;
            statusHistory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var statusHistoryDto = _mapper.Map<ApplicationStatusHistoryDto>(statusHistory);

            _logger.LogInformation("Updated status history with ID {Id}", id);

            return ServiceResult<ApplicationStatusHistoryDto>.Success(statusHistoryDto,
                "Historique de statut mis à jour avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status history with ID {Id}", id);
            return ServiceResult<ApplicationStatusHistoryDto>.InternalError(
                "Une erreur est survenue lors de la mise à jour de l'historique de statut");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var statusHistory = await _context.ApplicationStatusHistories.FindAsync(id);
            if (statusHistory == null)
            {
                return ServiceResult.NotFound($"Historique de statut avec l'ID {id} introuvable");
            }

            _context.ApplicationStatusHistories.Remove(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted status history with ID {Id}", id);

            return ServiceResult.Success("Historique de statut supprimé avec succès");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting status history with ID {Id}", id);
            return ServiceResult.InternalError("Une erreur est survenue lors de la suppression de l'historique de statut");
        }
    }
}