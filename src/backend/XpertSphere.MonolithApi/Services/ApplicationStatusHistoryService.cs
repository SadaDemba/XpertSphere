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
    private readonly ILogger<ApplicationStatusHistoryService> _logger;

    public ApplicationStatusHistoryService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<AddStatusChangeDto> addStatusChangeValidator,
        ILogger<ApplicationStatusHistoryService> logger)
    {
        _context = context;
        _mapper = mapper;
        _addStatusChangeValidator = addStatusChangeValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<ApplicationStatusHistoryDto>> AddStatusChangeAsync(AddStatusChangeDto addStatusChangeDto)
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
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound($"Application with ID {addStatusChangeDto.ApplicationId} not found");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == addStatusChangeDto.UpdatedByUserId);

            if (user == null)
            {
                return ServiceResult<ApplicationStatusHistoryDto>.NotFound($"User with ID {addStatusChangeDto.UpdatedByUserId} not found");
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

            return ServiceResult<ApplicationStatusHistoryDto>.Success(_mapper.Map<ApplicationStatusHistoryDto>(statusHistory),"Status change added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error adding status change for application {ApplicationId}",
                addStatusChangeDto.ApplicationId);
            return ServiceResult<ApplicationStatusHistoryDto>.InternalError("An error occurred while adding status change");
        }
    }

    public async Task<ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>> GetHistoryAsync(Guid applicationId)
    {
        try
        {
            var history = await _context.ApplicationStatusHistories
                .Include(h => h.UpdatedByUser)
                .Where(h => h.ApplicationId == applicationId)
                .OrderBy(h => h.UpdatedAt)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} status history entries for application {ApplicationId}",
                history.Count,
                applicationId);

            var historiesDto = _mapper.Map<IEnumerable<ApplicationStatusHistoryDto>>(history);

            return ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>.Success(historiesDto, "Application history retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving status history for application {ApplicationId}",
                applicationId);
            return ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>.InternalError("An error occurred while retrieving status history");
        }
    }
}