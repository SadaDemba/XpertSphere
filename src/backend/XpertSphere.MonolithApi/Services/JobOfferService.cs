using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.JobOffer;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class JobOfferService : IJobOfferService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateJobOfferDto> _createJobOfferValidator;
    private readonly IValidator<UpdateJobOfferDto> _updateJobOfferValidator;
    private readonly IValidator<JobOfferFilterDto> _filterValidator;
    private readonly ILogger<JobOfferService> _logger;

    public JobOfferService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<CreateJobOfferDto> createJobOfferValidator,
        IValidator<UpdateJobOfferDto> updateJobOfferValidator,
        IValidator<JobOfferFilterDto> filterValidator,
        ILogger<JobOfferService> logger)
    {
        _context = context;
        _mapper = mapper;
        _createJobOfferValidator = createJobOfferValidator;
        _updateJobOfferValidator = updateJobOfferValidator;
        _filterValidator = filterValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<JobOfferDto>>> GetAllJobOffersAsync()
    {
        try
        {
            var jobOffers = await _context.JobOffers
                .Include(jo => jo.Organization)
                .Include(jo => jo.CreatedByUserNavigation)
                .OrderByDescending(jo => jo.CreatedAt)
                .ToListAsync();

            var jobOfferDtos = _mapper.Map<IEnumerable<JobOfferDto>>(jobOffers);
            return ServiceResult<IEnumerable<JobOfferDto>>.Success(jobOfferDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all job offers");
            return ServiceResult<IEnumerable<JobOfferDto>>.InternalError("An error occurred while retrieving job offers");
        }
    }

    public async Task<PaginatedResult<JobOfferDto>> GetAllPaginatedJobOffersAsync(JobOfferFilterDto filter)
    {
        try
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return PaginatedResult<JobOfferDto>.Failure(errors, "Invalid filter parameters");
            }

            var query = BuildJobOfferQuery(filter);

            var pageNumber = int.TryParse(filter.PageNumber, out var pn) ? pn : 1;
            var pageSize = int.TryParse(filter.PageSize, out var ps) ? ps : 10;

            var paginatedResult = await query.ToPaginatedResultAsync(pageNumber, pageSize);

            return paginatedResult.Map(jobOffer => _mapper.Map<JobOfferDto>(jobOffer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated job offers with filter {Filter}", filter);
            return PaginatedResult<JobOfferDto>.Failure("An error occurred while searching job offers");
        }
    }

    public async Task<ServiceResult<JobOfferDto>> GetJobOfferByIdAsync(Guid id)
    {
        try
        {
            var jobOffer = await _context.JobOffers
                .Include(jo => jo.Organization)
                .Include(jo => jo.CreatedByUserNavigation)
                .FirstOrDefaultAsync(jo => jo.Id == id);

            if (jobOffer == null)
            {
                return ServiceResult<JobOfferDto>.NotFound($"Job offer with ID {id} not found");
            }

            var jobOfferDto = _mapper.Map<JobOfferDto>(jobOffer);
            return ServiceResult<JobOfferDto>.Success(jobOfferDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job offer with ID {JobOfferId}", id);
            return ServiceResult<JobOfferDto>.InternalError("An error occurred while retrieving the job offer");
        }
    }

    public async Task<ServiceResult<JobOfferDto>> CreateJobOfferAsync(CreateJobOfferDto createJobOfferDto, Guid userId, Guid organizationId)
    {
        try
        {
            var validationResult = await _createJobOfferValidator.ValidateAsync(createJobOfferDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<JobOfferDto>.ValidationError(errors);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);
            if (user == null)
            {
                return ServiceResult<JobOfferDto>.Forbidden("User does not belong to the specified organization");
            }

            var jobOffer = _mapper.Map<JobOffer>(createJobOfferDto);
            jobOffer.Id = Guid.NewGuid();
            jobOffer.OrganizationId = organizationId;
            jobOffer.CreatedByUserId = userId;
            jobOffer.Status = JobOfferStatus.Draft;

            try
            {
                jobOffer.Validate();
            }
            catch (ValidationException ex)
            {
                return ServiceResult<JobOfferDto>.ValidationError(new List<string> { ex.Message });
            }

            _context.JobOffers.Add(jobOffer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new job offer with ID {JobOfferId} for organization {OrganizationId}", jobOffer.Id, organizationId);

            var jobOfferDto = _mapper.Map<JobOfferDto>(jobOffer);
            return ServiceResult<JobOfferDto>.Success(jobOfferDto, "Job offer created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job offer for organization {OrganizationId}", organizationId);
            return ServiceResult<JobOfferDto>.InternalError("An error occurred while creating the job offer");
        }
    }

    public async Task<ServiceResult<JobOfferDto>> UpdateJobOfferAsync(Guid id, UpdateJobOfferDto updateJobOfferDto, Guid userId)
    {
        try
        {
            var validationResult = await _updateJobOfferValidator.ValidateAsync(updateJobOfferDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<JobOfferDto>.ValidationError(errors);
            }

            var jobOffer = await _context.JobOffers
                .Include(jo => jo.Organization)
                .Include(jo => jo.CreatedByUserNavigation)
                .FirstOrDefaultAsync(jo => jo.Id == id);

            if (jobOffer == null)
            {
                return ServiceResult<JobOfferDto>.NotFound($"Job offer with ID {id} not found");
            }

            var canManage = await CanUserManageJobOfferInternalAsync(jobOffer, userId);
            if (!canManage)
            {
                return ServiceResult<JobOfferDto>.Forbidden("User cannot manage this job offer");
            }

            _mapper.Map(updateJobOfferDto, jobOffer);

            try
            {
                jobOffer.Validate();
            }
            catch (ValidationException ex)
            {
                return ServiceResult<JobOfferDto>.ValidationError(new List<string> { ex.Message });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated job offer with ID {JobOfferId}", id);

            var jobOfferDto = _mapper.Map<JobOfferDto>(jobOffer);
            return ServiceResult<JobOfferDto>.Success(jobOfferDto, "Job offer updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job offer with ID {JobOfferId}", id);
            return ServiceResult<JobOfferDto>.InternalError("An error occurred while updating the job offer");
        }
    }

    public async Task<ServiceResult> DeleteJobOfferAsync(Guid id, Guid userId)
    {
        try
        {
            var jobOffer = await _context.JobOffers.FirstOrDefaultAsync(jo => jo.Id == id);
            if (jobOffer == null)
            {
                return ServiceResult.NotFound($"Job offer with ID {id} not found");
            }

            var canManage = await CanUserManageJobOfferInternalAsync(jobOffer, userId);
            if (!canManage)
            {
                return ServiceResult.Forbidden("User cannot manage this job offer");
            }

            if (jobOffer.Status == JobOfferStatus.Published)
            {
                return ServiceResult.Failure("Cannot delete published job offers. Close the job offer first.");
            }

            _context.JobOffers.Remove(jobOffer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted job offer with ID {JobOfferId}", id);
            return ServiceResult.Success("Job offer deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job offer with ID {JobOfferId}", id);
            return ServiceResult.InternalError("An error occurred while deleting the job offer");
        }
    }

    public async Task<ServiceResult> PublishJobOfferAsync(Guid id, Guid userId)
    {
        try
        {
            var jobOffer = await _context.JobOffers.FirstOrDefaultAsync(jo => jo.Id == id);
            if (jobOffer == null)
            {
                return ServiceResult.NotFound($"Job offer with ID {id} not found");
            }

            var canManage = await CanUserManageJobOfferInternalAsync(jobOffer, userId);
            if (!canManage)
            {
                return ServiceResult.Forbidden("User cannot manage this job offer");
            }

            try
            {
                jobOffer.Publish();
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Published job offer with ID {JobOfferId}", id);
            return ServiceResult.Success("Job offer published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing job offer with ID {JobOfferId}", id);
            return ServiceResult.InternalError("An error occurred while publishing the job offer");
        }
    }

    public async Task<ServiceResult> CloseJobOfferAsync(Guid id, Guid userId)
    {
        try
        {
            var jobOffer = await _context.JobOffers.FirstOrDefaultAsync(jo => jo.Id == id);
            if (jobOffer == null)
            {
                return ServiceResult.NotFound($"Job offer with ID {id} not found");
            }

            var canManage = await CanUserManageJobOfferInternalAsync(jobOffer, userId);
            if (!canManage)
            {
                return ServiceResult.Forbidden("User cannot manage this job offer");
            }

            try
            {
                jobOffer.Close();
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Closed job offer with ID {JobOfferId}", id);
            return ServiceResult.Success("Job offer closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing job offer with ID {JobOfferId}", id);
            return ServiceResult.InternalError("An error occurred while closing the job offer");
        }
    }

    public async Task<ServiceResult<IEnumerable<JobOfferDto>>> GetJobOffersByOrganizationAsync(Guid organizationId)
    {
        try
        {
            var jobOffers = await _context.JobOffers
                .Include(jo => jo.Organization)
                .Include(jo => jo.CreatedByUserNavigation)
                .Where(jo => jo.OrganizationId == organizationId)
                .OrderByDescending(jo => jo.CreatedAt)
                .ToListAsync();

            var jobOfferDtos = _mapper.Map<IEnumerable<JobOfferDto>>(jobOffers);
            return ServiceResult<IEnumerable<JobOfferDto>>.Success(jobOfferDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job offers for organization {OrganizationId}", organizationId);
            return ServiceResult<IEnumerable<JobOfferDto>>.InternalError("An error occurred while retrieving job offers");
        }
    }

    public async Task<ServiceResult<IEnumerable<JobOfferDto>>> GetJobOffersByUserAsync(Guid userId)
    {
        try
        {
            var jobOffers = await _context.JobOffers
                .Include(jo => jo.Organization)
                .Include(jo => jo.CreatedByUserNavigation)
                .Where(jo => jo.CreatedByUserId == userId)
                .OrderByDescending(jo => jo.CreatedAt)
                .ToListAsync();

            var jobOfferDtos = _mapper.Map<IEnumerable<JobOfferDto>>(jobOffers);
            return ServiceResult<IEnumerable<JobOfferDto>>.Success(jobOfferDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job offers for user {UserId}", userId);
            return ServiceResult<IEnumerable<JobOfferDto>>.InternalError("An error occurred while retrieving job offers");
        }
    }

    public async Task<ServiceResult<bool>> CanUserManageJobOfferAsync(Guid jobOfferId, Guid userId)
    {
        try
        {
            var jobOffer = await _context.JobOffers.FirstOrDefaultAsync(jo => jo.Id == jobOfferId);
            if (jobOffer == null)
            {
                return ServiceResult<bool>.NotFound($"Job offer with ID {jobOfferId} not found");
            }

            var canManage = await CanUserManageJobOfferInternalAsync(jobOffer, userId);
            return ServiceResult<bool>.Success(canManage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can manage job offer {JobOfferId}", jobOfferId);
            return ServiceResult<bool>.InternalError("An error occurred while checking permissions");
        }
    }

    private async Task<bool> CanUserManageJobOfferInternalAsync(JobOffer jobOffer, Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        return jobOffer.CreatedByUserId == userId || 
               (user.OrganizationId.HasValue && user.OrganizationId == jobOffer.OrganizationId);
    }

    private IQueryable<JobOffer> BuildJobOfferQuery(JobOfferFilterDto filter)
    {
        var query = _context.JobOffers
            .Include(jo => jo.Organization)
            .Include(jo => jo.CreatedByUserNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Title))
        {
            query = query.Where(jo => jo.Title.Contains(filter.Title, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrEmpty(filter.Location))
        {
            query = query.Where(jo => jo.Location != null && jo.Location.Contains(filter.Location, StringComparison.CurrentCultureIgnoreCase));
        }

        if (filter.WorkMode.HasValue)
        {
            query = query.Where(jo => jo.WorkMode == filter.WorkMode);
        }

        if (filter.ContractType.HasValue)
        {
            query = query.Where(jo => jo.ContractType == filter.ContractType);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(jo => jo.Status == filter.Status);
        }

        if (filter.SalaryMin.HasValue)
        {
            query = query.Where(jo => jo.SalaryMax == null || jo.SalaryMax >= filter.SalaryMin);
        }

        if (filter.SalaryMax.HasValue)
        {
            query = query.Where(jo => jo.SalaryMin == null || jo.SalaryMin <= filter.SalaryMax);
        }

        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(jo => jo.OrganizationId == filter.OrganizationId);
        }

        if (filter.CreatedByUserId.HasValue)
        {
            query = query.Where(jo => jo.CreatedByUserId == filter.CreatedByUserId);
        }

        if (filter.IsActive.HasValue)
        {
            query = filter.IsActive.Value 
                ? query.Where(jo => jo.Status == JobOfferStatus.Published && (jo.ExpiresAt == null || jo.ExpiresAt > DateTime.UtcNow))
                : query.Where(jo => jo.Status != JobOfferStatus.Published || (jo.ExpiresAt != null && jo.ExpiresAt <= DateTime.UtcNow));
        }

        if (filter.IsExpired.HasValue)
        {
            query = filter.IsExpired.Value
                ? query.Where(jo => jo.ExpiresAt != null && jo.ExpiresAt <= DateTime.UtcNow)
                : query.Where(jo => jo.ExpiresAt == null || jo.ExpiresAt > DateTime.UtcNow);
        }

        if (filter.PublishedAfter.HasValue)
        {
            query = query.Where(jo => jo.PublishedAt != null && jo.PublishedAt >= filter.PublishedAfter);
        }

        if (filter.PublishedBefore.HasValue)
        {
            query = query.Where(jo => jo.PublishedAt != null && jo.PublishedAt <= filter.PublishedBefore);
        }

        if (filter.ExpiresAfter.HasValue)
        {
            query = query.Where(jo => jo.ExpiresAt != null && jo.ExpiresAt >= filter.ExpiresAfter);
        }

        if (filter.ExpiresBefore.HasValue)
        {
            query = query.Where(jo => jo.ExpiresAt != null && jo.ExpiresAt <= filter.ExpiresBefore);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            query = query.Where(jo =>
                jo.Title.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                jo.Description.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                jo.Requirements.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                (jo.Location != null && jo.Location.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            query = ApplySorting(query, filter.SortBy, filter.SortDirection);
        }
        else
        {
            query = filter.SortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.CreatedAt)
                : query.OrderByDescending(jo => jo.CreatedAt);
        }

        return query;
    }

    private static IQueryable<JobOffer> ApplySorting(IQueryable<JobOffer> query, string sortBy, SortDirection sortDirection)
    {
        return sortBy.ToLower() switch
        {
            "title" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.Title)
                : query.OrderByDescending(jo => jo.Title),
            "location" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.Location)
                : query.OrderByDescending(jo => jo.Location),
            "workmode" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.WorkMode)
                : query.OrderByDescending(jo => jo.WorkMode),
            "contracttype" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.ContractType)
                : query.OrderByDescending(jo => jo.ContractType),
            "status" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.Status)
                : query.OrderByDescending(jo => jo.Status),
            "publishedat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.PublishedAt)
                : query.OrderByDescending(jo => jo.PublishedAt),
            "expiresat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.ExpiresAt)
                : query.OrderByDescending(jo => jo.ExpiresAt),
            "createdat" => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.CreatedAt)
                : query.OrderByDescending(jo => jo.CreatedAt),
            _ => sortDirection == SortDirection.Ascending
                ? query.OrderBy(jo => jo.CreatedAt)
                : query.OrderByDescending(jo => jo.CreatedAt)
        };
    }
}