using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Enums;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Services.Pagination;
using XpertSphere.MonolithApi.Utils;
using XpertSphere.MonolithApi.Utils.Pagination;

namespace XpertSphere.MonolithApi.Services
{
    public class OrganizationService( XpertSphereDbContext context) : IOrganizationService
    {
        private readonly XpertSphereDbContext _context = context;

        public async Task<Organization> Post(Organization organization)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
            return organization;
        }

        public async  Task<bool> Delete(Guid id)
        {
            var organization = await _context.Organizations.AsNoTracking()
                .Include(o => o.Users)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null) throw new KeyNotFoundException(Constants.COMPANY_NOT_FOUND);

            _context.Organizations.Remove(organization);
            await _context.SaveChangesAsync();
            return true ;
        }

        public async Task<Organization> Get(Guid organizationId)
        {
            var organization = await _context.Organizations.AsNoTracking()
                .Include(o => o.Users)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefaultAsync(o => o.Id == organizationId);
            return organization ?? throw new KeyNotFoundException(Constants.COMPANY_NOT_FOUND);
        }

        public async Task<ResponseResource<Organization>> GetAll(OrganizationFilterDto organizationFilterDto)
        {
            IQueryable<Organization> source = _context.Organizations
               .Include(o => o.Users)
               .ThenInclude(u => u.UserRoles)
               .AsQueryable();

            var filterdData = ApplyFilters(source, organizationFilterDto);

            return await PaginationService<Organization>.Paginate(organizationFilterDto.PageNumber, organizationFilterDto.PageSize, filterdData);

        }

        public async Task<Organization> Put(Guid id, Organization organization)
        {
            if (organization.Id != id) throw new InvalidDataException(Constants.INVALID_ID);

            if (await _context.Organizations.AsNoTracking().AnyAsync(o => o.Id == id)) throw new KeyNotFoundException(Constants.COMPANY_NOT_FOUND);

            organization.UpdatedAt = DateTime.UtcNow;

            _context.Entry(organization).State = EntityState.Modified;
            _context.Entry(organization).Property(a => a.CreatedAt).IsModified = false;

            await _context.SaveChangesAsync();
            return organization;
        }

        private static IQueryable<Organization> ApplyFilters(IQueryable<Organization> query, OrganizationFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerms))
            {
                filter.SearchTerms = filter.SearchTerms.ToLower();
                query = query.Where(o =>
                    o.Name.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                    o.Address.ToString().Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                    o.Code.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                    o.ContactEmail!.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase)
                );
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive);
            }

            var isDescending = filter.SortDirection == SortDirection.Descending;

            return filter.SortBy?.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                "industry" => isDescending ? query.OrderByDescending(u => u.Industry) : query.OrderBy(u => u.Industry),
                "code" => isDescending ? query.OrderByDescending(u => u.Code) : query.OrderBy(u => u.Code),
                "contactemail" => isDescending ? query.OrderByDescending(u => u.ContactEmail) : query.OrderBy(u => u.ContactEmail),
                "createdat" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                _ => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
            };
        }

    }
}
