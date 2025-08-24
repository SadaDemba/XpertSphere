using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.Application;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;
using XpertSphere.MonolithApi.Utils.Results.Pagination;

namespace XpertSphere.MonolithApi.Services;

public class ApplicationService : IApplicationService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateApplicationDto> _createApplicationValidator;
    private readonly IValidator<UpdateApplicationDto> _updateApplicationValidator;
    private readonly IValidator<UpdateApplicationStatusDto> _updateStatusValidator;
    private readonly IValidator<ApplicationFilterDto> _filterValidator;
    private readonly IValidator<AssignUserDto> _assignUserValidator;
    private readonly IApplicationStatusHistoryService _statusHistoryService;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<CreateApplicationDto> createApplicationValidator,
        IValidator<UpdateApplicationDto> updateApplicationValidator,
        IValidator<UpdateApplicationStatusDto> updateStatusValidator,
        IValidator<ApplicationFilterDto> filterValidator,
        IValidator<AssignUserDto> assignUserValidator,
        IApplicationStatusHistoryService statusHistoryService,
        ILogger<ApplicationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _createApplicationValidator = createApplicationValidator;
        _updateApplicationValidator = updateApplicationValidator;
        _updateStatusValidator = updateStatusValidator;
        _filterValidator = filterValidator;
        _assignUserValidator = assignUserValidator;
        _statusHistoryService = statusHistoryService;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<ApplicationDto>>> GetAllApplicationsAsync()
    {
        try
        {
            var applications = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var applicationDtos = _mapper.Map<IEnumerable<ApplicationDto>>(applications);
            return ServiceResult<IEnumerable<ApplicationDto>>.Success(applicationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all applications");
            return ServiceResult<IEnumerable<ApplicationDto>>.InternalError("An error occurred while retrieving applications");
        }
    }

    public async Task<PaginatedResult<ApplicationDto>> GetAllPaginatedApplicationsAsync(ApplicationFilterDto filter)
    {
        try
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return PaginatedResult<ApplicationDto>.Failure(errors, "Invalid filter parameters");
            }

            var query = BuildApplicationQuery(filter);

            var pageNumber = int.TryParse(filter.PageNumber, out var pn) ? pn : 1;
            var pageSize = int.TryParse(filter.PageSize, out var ps) ? ps : 10;

            var paginatedResult = await query.ToPaginatedResultAsync(pageNumber, pageSize);

            return paginatedResult.Map(application => _mapper.Map<ApplicationDto>(application));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated applications with filter {Filter}", filter);
            return PaginatedResult<ApplicationDto>.Failure("An error occurred while searching applications");
        }
    }

    public async Task<ServiceResult<ApplicationDto>> GetApplicationByIdAsync(Guid id)
    {
        try
        {
            var application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(jo => jo.StatusHistory)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Application with ID {id} not found");
            }

            var applicationDto = _mapper.Map<ApplicationDto>(application);
            return ServiceResult<ApplicationDto>.Success(applicationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application with ID {ApplicationId}", id);
            return ServiceResult<ApplicationDto>.InternalError("An error occurred while retrieving the application");
        }
    }

    public async Task<ServiceResult<ApplicationDto>> CreateApplicationAsync(CreateApplicationDto createApplicationDto, Guid candidateId)
    {
        try
        {
            var validationResult = await _createApplicationValidator.ValidateAsync(createApplicationDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationDto>.ValidationError(errors);
            }

            // Check if job offer exists and is published
            var jobOffer = await _context.JobOffers
                .Include(jo => jo.Organization)
                .FirstOrDefaultAsync(jo => jo.Id == createApplicationDto.JobOfferId);

            if (jobOffer == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Job offer with ID {createApplicationDto.JobOfferId} not found");
            }

            if (jobOffer.Status != JobOfferStatus.Published)
            {
                return ServiceResult<ApplicationDto>.Failure("Cannot apply to unpublished job offers");
            }

            if (jobOffer.IsExpired)
            {
                return ServiceResult<ApplicationDto>.Failure("Cannot apply to expired job offers");
            }

            // Check if candidate exists
            var candidate = await _context.Users.FirstOrDefaultAsync(u => u.Id == candidateId);
            if (candidate == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Candidate with ID {candidateId} not found");
            }

            // Check if candidate has already applied to this job offer
            var existingApplication = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobOfferId == createApplicationDto.JobOfferId && a.CandidateId == candidateId);

            if (existingApplication != null)
            {
                return ServiceResult<ApplicationDto>.Conflict("You have already applied to this job offer");
            }

            var application = _mapper.Map<Application>(createApplicationDto);
            application.Id = Guid.NewGuid();
            application.CandidateId = candidateId;
            application.CurrentStatus = ApplicationStatus.Applied;
            application.AppliedAt = DateTime.UtcNow;
            application.LastUpdatedAt = DateTime.UtcNow;

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Add initial status history
            var addStatusChangeDto = new AddStatusChangeDto
            {
                ApplicationId = application.Id,
                Status = ApplicationStatus.Applied,
                Comment = "Application submitted by candidate",
                UpdatedByUserId = candidateId
            };

            await _statusHistoryService.AddStatusChangeAsync(addStatusChangeDto);

            _logger.LogInformation("Created new application with ID {ApplicationId} for job offer {JobOfferId}", 
                application.Id, createApplicationDto.JobOfferId);

            // Reload with includes for proper mapping
            application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .FirstAsync(a => a.Id == application.Id);

            var applicationDto = _mapper.Map<ApplicationDto>(application);
            return ServiceResult<ApplicationDto>.Success(applicationDto, "Application submitted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application for job offer {JobOfferId}", createApplicationDto.JobOfferId);
            return ServiceResult<ApplicationDto>.InternalError("An error occurred while creating the application");
        }
    }

    public async Task<ServiceResult<ApplicationDto>> UpdateApplicationAsync(Guid id, UpdateApplicationDto updateApplicationDto, Guid userId)
    {
        try
        {
            var validationResult = await _updateApplicationValidator.ValidateAsync(updateApplicationDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationDto>.ValidationError(errors);
            }

            var application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Application with ID {id} not found");
            }

            var canManage = await CanUserManageApplicationInternalAsync(application, userId);
            if (!canManage)
            {
                return ServiceResult<ApplicationDto>.Forbidden("User cannot manage this application");
            }

            _mapper.Map(updateApplicationDto, application);
            application.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated application with ID {ApplicationId}", id);

            var applicationDto = _mapper.Map<ApplicationDto>(application);
            return ServiceResult<ApplicationDto>.Success(applicationDto, "Application updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application with ID {ApplicationId}", id);
            return ServiceResult<ApplicationDto>.InternalError("An error occurred while updating the application");
        }
    }

    public async Task<ServiceResult> DeleteApplicationAsync(Guid id, Guid userId)
    {
        try
        {
            var application = await _context.Applications
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return ServiceResult.NotFound($"Application with ID {id} not found");
            }

            var canManage = await CanUserManageApplicationInternalAsync(application, userId);
            if (!canManage)
            {
                return ServiceResult.Forbidden("User cannot manage this application");
            }

            if (application.IsCompleted)
            {
                return ServiceResult.Failure("Cannot delete completed applications");
            }

            // Status history will be cascade deleted due to relationship configuration
            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted application with ID {ApplicationId}", id);
            return ServiceResult.Success("Application deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application with ID {ApplicationId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the application");
        }
    }

    public async Task<ServiceResult<ApplicationDto>> UpdateApplicationStatusAsync(Guid id, UpdateApplicationStatusDto updateStatusDto, Guid userId)
    {
        try
        {
            var validationResult = await _updateStatusValidator.ValidateAsync(updateStatusDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationDto>.ValidationError(errors);
            }

            var application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Application with ID {id} not found");
            }

            var canManage = await CanUserManageApplicationInternalAsync(application, userId);
            if (!canManage)
            {
                return ServiceResult<ApplicationDto>.Forbidden("User cannot manage this application");
            }

            if (application.IsCompleted && updateStatusDto.Status != ApplicationStatus.Withdrawn)
            {
                return ServiceResult<ApplicationDto>.Failure("Cannot update status of completed application");
            }

            // Validate status transition (this could be expanded with business rules)
            if (updateStatusDto.Status == ApplicationStatus.Applied)
            {
                return ServiceResult<ApplicationDto>.Failure("Cannot change status back to Applied");
            }

            // Add status change to history
            var addStatusChangeDto = new AddStatusChangeDto
            {
                ApplicationId = application.Id,
                Status = updateStatusDto.Status,
                Comment = updateStatusDto.Comment,
                Rating = updateStatusDto.Rating,
                UpdatedByUserId = userId
            };

            var historyResult = await _statusHistoryService.AddStatusChangeAsync(addStatusChangeDto);
            if (!historyResult.IsSuccess)
            {
                return ServiceResult<ApplicationDto>.Failure("Failed to add status change to history");
            }

            // Update application
            application.CurrentStatus = updateStatusDto.Status;
            application.LastUpdatedAt = DateTime.UtcNow;
            if (updateStatusDto.Rating.HasValue)
            {
                application.Rating = updateStatusDto.Rating;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated status for application {ApplicationId} to {Status}", id, updateStatusDto.Status);

            // Reload to get updated status history
            application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .FirstAsync(a => a.Id == id);

            var applicationDto = _mapper.Map<ApplicationDto>(application);
            return ServiceResult<ApplicationDto>.Success(applicationDto, "Application status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for application {ApplicationId}", id);
            return ServiceResult<ApplicationDto>.InternalError("An error occurred while updating application status");
        }
    }

    public async Task<ServiceResult> WithdrawApplicationAsync(Guid id, string reason, Guid candidateId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResult.ValidationError(["Reason is required when withdrawing application"]);
            }

            var application = await _context.Applications.FirstOrDefaultAsync(a => a.Id == id);
            if (application == null)
            {
                return ServiceResult.NotFound($"Application with ID {id} not found");
            }

            if (application.CandidateId != candidateId)
            {
                return ServiceResult.Forbidden("Only the candidate can withdraw their application");
            }

            if (application.IsCompleted)
            {
                return ServiceResult.Failure("Cannot withdraw completed application");
            }

            var updateStatusDto = new UpdateApplicationStatusDto
            {
                Status = ApplicationStatus.Withdrawn,
                Comment = reason
            };

            var result = await UpdateApplicationStatusAsync(id, updateStatusDto, candidateId);
            if (!result.IsSuccess)
            {
                return ServiceResult.Failure("Failed to withdraw application");
            }

            _logger.LogInformation("Application {ApplicationId} withdrawn by candidate {CandidateId}", id, candidateId);
            return ServiceResult.Success("Application withdrawn successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing application {ApplicationId}", id);
            return ServiceResult.InternalError("An error occurred while withdrawing the application");
        }
    }

    public async Task<ServiceResult<IEnumerable<ApplicationDto>>> GetApplicationsByJobOfferAsync(Guid jobOfferId)
    {
        try
        {
            var applications = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .Where(a => a.JobOfferId == jobOfferId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var applicationDtos = _mapper.Map<IEnumerable<ApplicationDto>>(applications);
            return ServiceResult<IEnumerable<ApplicationDto>>.Success(applicationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications for job offer {JobOfferId}", jobOfferId);
            return ServiceResult<IEnumerable<ApplicationDto>>.InternalError("An error occurred while retrieving applications");
        }
    }

    public async Task<ServiceResult<IEnumerable<ApplicationDto>>> GetApplicationsByCandidateAsync(Guid candidateId)
    {
        try
        {
            var applications = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .Where(a => a.CandidateId == candidateId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var applicationDtos = _mapper.Map<IEnumerable<ApplicationDto>>(applications);
            return ServiceResult<IEnumerable<ApplicationDto>>.Success(applicationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications for candidate {CandidateId}", candidateId);
            return ServiceResult<IEnumerable<ApplicationDto>>.InternalError("An error occurred while retrieving applications");
        }
    }

    public async Task<ServiceResult<IEnumerable<ApplicationDto>>> GetApplicationsByOrganizationAsync(Guid organizationId)
    {
        try
        {
            var applications = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .Include(a => a.StatusHistory)
                    .ThenInclude(h => h.UpdatedByUser)
                .Where(a => a.JobOffer.OrganizationId == organizationId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            var applicationDtos = _mapper.Map<IEnumerable<ApplicationDto>>(applications);
            return ServiceResult<IEnumerable<ApplicationDto>>.Success(applicationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications for organization {OrganizationId}", organizationId);
            return ServiceResult<IEnumerable<ApplicationDto>>.InternalError("An error occurred while retrieving applications");
        }
    }

    public async Task<ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>> GetApplicationStatusHistoryAsync(Guid applicationId)
    {
        try
        {
            var application = await _context.Applications.FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
            {
                return ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>.NotFound($"Application with ID {applicationId} not found");
            }

            return await _statusHistoryService.GetByApplicationIdAsync(applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status history for application {ApplicationId}", applicationId);
            return ServiceResult<IEnumerable<ApplicationStatusHistoryDto>>.InternalError("An error occurred while retrieving status history");
        }
    }

    public async Task<ServiceResult<bool>> CanUserManageApplicationAsync(Guid applicationId, Guid userId)
    {
        try
        {
            var application = await _context.Applications
                .Include(a => a.JobOffer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                return ServiceResult<bool>.NotFound($"Application with ID {applicationId} not found");
            }

            var canManage = await CanUserManageApplicationInternalAsync(application, userId);
            return ServiceResult<bool>.Success(canManage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can manage application {ApplicationId}", applicationId);
            return ServiceResult<bool>.InternalError("An error occurred while checking permissions");
        }
    }

    public async Task<ServiceResult<bool>> HasCandidateAppliedToJobAsync(Guid jobOfferId, Guid candidateId)
    {
        try
        {
            var hasApplied = await _context.Applications
                .AnyAsync(a => a.JobOfferId == jobOfferId && a.CandidateId == candidateId);

            return ServiceResult<bool>.Success(hasApplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if candidate has applied to job {JobOfferId}", jobOfferId);
            return ServiceResult<bool>.InternalError("An error occurred while checking application status");
        }
    }

    private async Task<bool> CanUserManageApplicationInternalAsync(Application application, Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        // Candidate can manage their own application
        if (application.CandidateId == userId) return true;

        // Organization members can manage applications for their job offers
        if (user.OrganizationId.HasValue && user.OrganizationId == application.JobOffer.OrganizationId)
            return true;

        return false;
    }

    private IQueryable<Application> BuildApplicationQuery(ApplicationFilterDto filter)
    {
        var query = _context.Applications
            .Include(a => a.JobOffer)
                .ThenInclude(jo => jo.Organization)
            .Include(a => a.Candidate)
            .Include(a => a.AssignedTechnicalEvaluator)
            .Include(a => a.AssignedManager)
            .Include(a => a.StatusHistory)
                .ThenInclude(h => h.UpdatedByUser)
            .AsQueryable();

        if (filter.JobOfferId.HasValue)
        {
            query = query.Where(a => a.JobOfferId == filter.JobOfferId);
        }

        if (filter.CandidateId.HasValue)
        {
            query = query.Where(a => a.CandidateId == filter.CandidateId);
        }

        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(a => a.JobOffer.OrganizationId == filter.OrganizationId);
        }

        if (filter.CurrentStatus.HasValue)
        {
            query = query.Where(a => a.CurrentStatus == filter.CurrentStatus);
        }

        if (filter.MinRating.HasValue)
        {
            query = query.Where(a => a.Rating != null && a.Rating >= filter.MinRating);
        }

        if (filter.MaxRating.HasValue)
        {
            query = query.Where(a => a.Rating != null && a.Rating <= filter.MaxRating);
        }

        if (filter.IsActive.HasValue)
        {
            if (filter.IsActive.Value)
            {
                query = query.Where(a => a.CurrentStatus != ApplicationStatus.Rejected && 
                                        a.CurrentStatus != ApplicationStatus.Withdrawn && 
                                        a.CurrentStatus != ApplicationStatus.Accepted);
            }
            else
            {
                query = query.Where(a => a.CurrentStatus == ApplicationStatus.Rejected || 
                                        a.CurrentStatus == ApplicationStatus.Withdrawn || 
                                        a.CurrentStatus == ApplicationStatus.Accepted);
            }
        }

        if (filter.IsCompleted.HasValue)
        {
            if (filter.IsCompleted.Value)
            {
                query = query.Where(a => a.CurrentStatus == ApplicationStatus.Accepted || 
                                        a.CurrentStatus == ApplicationStatus.Rejected || 
                                        a.CurrentStatus == ApplicationStatus.Withdrawn);
            }
            else
            {
                query = query.Where(a => a.CurrentStatus != ApplicationStatus.Accepted && 
                                        a.CurrentStatus != ApplicationStatus.Rejected && 
                                        a.CurrentStatus != ApplicationStatus.Withdrawn);
            }
        }

        if (filter.IsInProgress.HasValue)
        {
            if (filter.IsInProgress.Value)
            {
                query = query.Where(a => a.CurrentStatus != ApplicationStatus.Applied && 
                                        a.CurrentStatus != ApplicationStatus.Rejected && 
                                        a.CurrentStatus != ApplicationStatus.Withdrawn && 
                                        a.CurrentStatus != ApplicationStatus.Accepted);
            }
            else
            {
                query = query.Where(a => a.CurrentStatus == ApplicationStatus.Applied || 
                                        a.CurrentStatus == ApplicationStatus.Rejected || 
                                        a.CurrentStatus == ApplicationStatus.Withdrawn || 
                                        a.CurrentStatus == ApplicationStatus.Accepted);
            }
        }

        if (filter.AppliedAfter.HasValue)
        {
            query = query.Where(a => a.AppliedAt >= filter.AppliedAfter);
        }

        if (filter.AppliedBefore.HasValue)
        {
            query = query.Where(a => a.AppliedAt <= filter.AppliedBefore);
        }

        if (filter.UpdatedAfter.HasValue)
        {
            query = query.Where(a => a.LastUpdatedAt != null && a.LastUpdatedAt >= filter.UpdatedAfter);
        }

        if (filter.UpdatedBefore.HasValue)
        {
            query = query.Where(a => a.LastUpdatedAt != null && a.LastUpdatedAt <= filter.UpdatedBefore);
        }

        if (!string.IsNullOrEmpty(filter.CandidateName))
        {
            query = query.Where(a => (a.Candidate.FirstName + " " + a.Candidate.LastName)
                .Contains(filter.CandidateName, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrEmpty(filter.JobTitle))
        {
            query = query.Where(a => a.JobOffer.Title.ToLower()
                .Contains(filter.JobTitle.ToLower()));
        }

        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            var searchTerms = filter.SearchTerms.ToLower();
            query = query.Where(a =>
                a.JobOffer.Title.ToLower().Contains(searchTerms) ||
                (a.Candidate.FirstName + " " + a.Candidate.LastName).ToLower().Contains(searchTerms) ||
                (a.CoverLetter != null && a.CoverLetter.ToLower().Contains(searchTerms)) ||
                (a.AdditionalNotes != null && a.AdditionalNotes.ToLower().Contains(searchTerms)));
        }

        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);
        }
        else
        {
            query = filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.AppliedAt)
                : query.OrderByDescending(a => a.AppliedAt);
        }

        return query;
    }

    private static IQueryable<Application> ApplySorting(IQueryable<Application> query, string sortBy, SortDirection sortDirection)
    {
        return sortBy.ToLower() switch
        {
            "candidatename" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.Candidate.FirstName).ThenBy(a => a.Candidate.LastName)
                : query.OrderByDescending(a => a.Candidate.FirstName).ThenByDescending(a => a.Candidate.LastName),
            "jobtitle" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.JobOffer.Title)
                : query.OrderByDescending(a => a.JobOffer.Title),
            "status" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.CurrentStatus)
                : query.OrderByDescending(a => a.CurrentStatus),
            "rating" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.Rating)
                : query.OrderByDescending(a => a.Rating),
            "appliedat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.AppliedAt)
                : query.OrderByDescending(a => a.AppliedAt),
            "lastupdatedat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.LastUpdatedAt)
                : query.OrderByDescending(a => a.LastUpdatedAt),
            "createdat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.CreatedAt)
                : query.OrderByDescending(a => a.CreatedAt),
            _ => sortDirection == SortDirection.Ascending
                ? query.OrderBy(a => a.AppliedAt)
                : query.OrderByDescending(a => a.AppliedAt)
        };
    }

    public async Task<ServiceResult<ApplicationDto>> AssignUserAsync(AssignUserDto assignUserDto, Guid assignedByUserId)
    {
        try
        {
            var validationResult = await _assignUserValidator.ValidateAsync(assignUserDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationDto>.ValidationError(errors);
            }

            var application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .FirstOrDefaultAsync(a => a.Id == assignUserDto.ApplicationId);

            if (application == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Application with ID {assignUserDto.ApplicationId} not found");
            }

            // Check if user can manage this application (recruiter of the organization)
            var canManage = await CanUserManageApplicationInternalAsync(application, assignedByUserId);
            if (!canManage)
            {
                return ServiceResult<ApplicationDto>.Forbidden("User cannot manage this application");
            }

            // Verify that the user belongs to the same organization as the job offer
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == assignUserDto.UserId);

            if (user == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"User with ID {assignUserDto.UserId} not found");
            }

            if (user.OrganizationId != application.JobOffer.OrganizationId)
            {
                return ServiceResult<ApplicationDto>.Failure("User must belong to the same organization as the job offer");
            }

            // Assign based on the assignment type
            if (assignUserDto.AssignmentType == AssignmentType.TechnicalEvaluator)
            {
                application.AssignedTechnicalEvaluatorId = assignUserDto.UserId;
            }
            else if (assignUserDto.AssignmentType == AssignmentType.Manager)
            {
                application.AssignedManagerId = assignUserDto.UserId;
            }

            application.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned {AssignmentType} to application {ApplicationId} by user {UserId}", 
                assignUserDto.AssignmentType, assignUserDto.ApplicationId, assignedByUserId);

            // Reload to get updated navigation properties
            application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .FirstAsync(a => a.Id == assignUserDto.ApplicationId);

            var applicationDto = _mapper.Map<ApplicationDto>(application);
            return ServiceResult<ApplicationDto>.Success(applicationDto, $"{assignUserDto.AssignmentType} assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning {AssignmentType} to application {ApplicationId}", assignUserDto.AssignmentType, assignUserDto.ApplicationId);
            return ServiceResult<ApplicationDto>.InternalError("An error occurred while assigning user");
        }
    }

    public async Task<ServiceResult<ApplicationDto>> UnassignUserAsync(AssignUserDto unassignUserDto, Guid assignedByUserId)
    {
        try
        {
            var validationResult = await _assignUserValidator.ValidateAsync(unassignUserDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<ApplicationDto>.ValidationError(errors);
            }

            var application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .FirstOrDefaultAsync(a => a.Id == unassignUserDto.ApplicationId);

            if (application == null)
            {
                return ServiceResult<ApplicationDto>.NotFound($"Application with ID {unassignUserDto.ApplicationId} not found");
            }

            // Check if user can manage this application (recruiter of the organization)
            var canManage = await CanUserManageApplicationInternalAsync(application, assignedByUserId);
            if (!canManage)
            {
                return ServiceResult<ApplicationDto>.Forbidden("User cannot manage this application");
            }

            // Unassign based on the assignment type
            if (unassignUserDto.AssignmentType == AssignmentType.TechnicalEvaluator)
            {
                if (!application.AssignedTechnicalEvaluatorId.HasValue)
                {
                    return ServiceResult<ApplicationDto>.Failure("No technical evaluator assigned to this application");
                }
                application.AssignedTechnicalEvaluatorId = null;
            }
            else if (unassignUserDto.AssignmentType == AssignmentType.Manager)
            {
                if (!application.AssignedManagerId.HasValue)
                {
                    return ServiceResult<ApplicationDto>.Failure("No manager assigned to this application");
                }
                application.AssignedManagerId = null;
            }

            application.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Unassigned {AssignmentType} from application {ApplicationId} by user {UserId}", 
                unassignUserDto.AssignmentType, unassignUserDto.ApplicationId, assignedByUserId);

            // Reload to get updated navigation properties
            application = await _context.Applications
                .Include(a => a.JobOffer)
                    .ThenInclude(jo => jo.Organization)
                .Include(a => a.Candidate)
                .Include(a => a.AssignedTechnicalEvaluator)
                .Include(a => a.AssignedManager)
                .FirstAsync(a => a.Id == unassignUserDto.ApplicationId);

            var applicationDto = _mapper.Map<ApplicationDto>(application);
            return ServiceResult<ApplicationDto>.Success(applicationDto, $"{unassignUserDto.AssignmentType} unassigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning {AssignmentType} from application {ApplicationId}", unassignUserDto.AssignmentType, unassignUserDto.ApplicationId);
            return ServiceResult<ApplicationDto>.InternalError("An error occurred while unassigning user");
        }
    }
}