using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Pagination;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly XpertSphereDbContext _context;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateOrganizationDto> _createValidator;
        private readonly IValidator<UpdateOrganizationDto> _updateValidator;
        private readonly IValidator<OrganizationFilterDto> _filterValidator;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(
            XpertSphereDbContext context,
            IMapper mapper,
            IValidator<CreateOrganizationDto> createValidator,
            IValidator<UpdateOrganizationDto> updateValidator,
            IValidator<OrganizationFilterDto> filterValidator,
            ILogger<OrganizationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _filterValidator = filterValidator;
            _logger = logger;
        }

        public async Task<ServiceResult<OrganizationDto>> CreateAsync(CreateOrganizationDto createDto)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(createDto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return ServiceResult<OrganizationDto>.ValidationError(errors);
                }

                // Check if organization with same name or code already exists
                var existingOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Name == createDto.Name || o.Code == createDto.Code);

                if (existingOrg != null)
                {
                    return ServiceResult<OrganizationDto>.Conflict($"An organization with name '{createDto.Name}' or code '{createDto.Code}' already exists");
                }

                var organization = _mapper.Map<Organization>(createDto);
                organization.Id = Guid.NewGuid();
                organization.IsActive = true;

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new organization with ID {OrganizationId} and name {OrganizationName}", organization.Id, organization.Name);

                var organizationDto = _mapper.Map<OrganizationDto>(organization);
                return ServiceResult<OrganizationDto>.Success(organizationDto, "Organization created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization with name {OrganizationName}", createDto.Name);
                return ServiceResult<OrganizationDto>.InternalError("An error occurred while creating the organization");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (organization == null)
                {
                    return ServiceResult.NotFound($"Organization with ID {id} not found");
                }

                // Check if organization has active users
                var hasActiveUsers = organization.Users.Any(u => u.IsActive);
                if (hasActiveUsers)
                {
                    return ServiceResult.Failure("Cannot delete organization that has active users");
                }

                _context.Organizations.Remove(organization);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted organization with ID {OrganizationId} and name {OrganizationName}", id, organization.Name);
                return ServiceResult.Success("Organization deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization with ID {OrganizationId}", id);
                return ServiceResult.InternalError("An error occurred while deleting the organization");
            }
        }

        public async Task<ServiceResult<OrganizationDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (organization == null)
                {
                    return ServiceResult<OrganizationDto>.NotFound($"Organization with ID {id} not found");
                }

                var organizationDto = _mapper.Map<OrganizationDto>(organization);
                return ServiceResult<OrganizationDto>.Success(organizationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization with ID {OrganizationId}", id);
                return ServiceResult<OrganizationDto>.InternalError("An error occurred while retrieving the organization");
            }
        }

        public async Task<PaginatedResult<OrganizationDto>> GetAllAsync(OrganizationFilterDto filter)
        {
            try
            {
                var validationResult = await _filterValidator.ValidateAsync(filter);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<OrganizationDto>.Failure(errors, "Invalid filter parameters");
                }

                var query = BuildOrganizationQuery(filter);
                
                var pageNumber = int.TryParse(filter.PageNumber, out var pn) ? pn : 1;
                var pageSize = int.TryParse(filter.PageSize, out var ps) ? ps : 10;

                var paginatedResult = await query.ToPaginatedResultAsync(pageNumber, pageSize);
                
                return paginatedResult.Map(org => _mapper.Map<OrganizationDto>(org));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizations with filter {Filter}", filter);
                return PaginatedResult<OrganizationDto>.Failure("An error occurred while searching organizations");
            }
        }

        public async Task<ServiceResult<OrganizationDto>> UpdateAsync(Guid id, UpdateOrganizationDto updateDto)
        {
            try
            {
                var validationResult = await _updateValidator.ValidateAsync(updateDto);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return ServiceResult<OrganizationDto>.ValidationError(errors);
                }

                var organization = await _context.Organizations.FindAsync(id);
                if (organization == null)
                {
                    return ServiceResult<OrganizationDto>.NotFound($"Organization with ID {id} not found");
                }
                
                if (!string.IsNullOrEmpty(updateDto.Name) && updateDto.Name != organization.Name)
                {
                    var nameExists = await _context.Organizations.AnyAsync(o => o.Name == updateDto.Name && o.Id != id);
                    if (nameExists)
                    {
                        return ServiceResult<OrganizationDto>.Conflict($"An organization with name '{updateDto.Name}' already exists");
                    }
                }

                if (!string.IsNullOrEmpty(updateDto.Code) && updateDto.Code != organization.Code)
                {
                    var codeExists = await _context.Organizations.AnyAsync(o => o.Code == updateDto.Code && o.Id != id);
                    if (codeExists)
                    {
                        return ServiceResult<OrganizationDto>.Conflict($"An organization with code '{updateDto.Code}' already exists");
                    }
                }

                _mapper.Map(updateDto, organization);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated organization with ID {OrganizationId}", id);

                var organizationDto = _mapper.Map<OrganizationDto>(organization);
                return ServiceResult<OrganizationDto>.Success(organizationDto, "Organization updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization with ID {OrganizationId}", id);
                return ServiceResult<OrganizationDto>.InternalError("An error occurred while updating the organization");
            }
        }

        private IQueryable<Organization> BuildOrganizationQuery(OrganizationFilterDto filter)
        {
            var query = _context.Organizations
                .Include(o => o.Users)
                .AsQueryable();
            
            // Apply filters
            if (filter.IsActive.HasValue)
            {
                query = query.Where(o => o.IsActive == filter.IsActive);
            }
            
            // Search terms
            if (!string.IsNullOrEmpty(filter.SearchTerms))
            {
                query = query.Where(o => 
                    o.Name.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                    o.Code.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                    o.ContactEmail!.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                    o.Industry!.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase)
                    );
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = ApplySorting(query, filter.SortBy, filter.SortDirection);
            }
            else
            {
                query = filter.SortDirection == SortDirection.Ascending
                    ? query.OrderBy(o => o.CreatedAt)
                    : query.OrderByDescending(o => o.CreatedAt);
            }

            return query;
        }
        
        private static IQueryable<Organization> ApplySorting(IQueryable<Organization> query, string sortBy, SortDirection sortDirection)
        {
            return sortBy.ToLower() switch
            {
                "name" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(o => o.Name) 
                    : query.OrderByDescending(o => o.Name),
                "code" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(o => o.Code) 
                    : query.OrderByDescending(o => o.Code),
                "industry" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(o => o.Industry) 
                    : query.OrderByDescending(o => o.Industry),
                "contactemail" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(o => o.ContactEmail) 
                    : query.OrderByDescending(o => o.ContactEmail),
                "createdat" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(o => o.CreatedAt) 
                    : query.OrderByDescending(o => o.CreatedAt),
                _ => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(o => o.CreatedAt) 
                    : query.OrderByDescending(o => o.CreatedAt)
                };
        }
        
    }
}
