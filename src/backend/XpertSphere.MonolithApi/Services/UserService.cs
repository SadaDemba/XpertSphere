using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;
using Elfie.Serialization;
using XpertSphere.MonolithApi.Services.Pagination;
using XpertSphere.MonolithApi.Utils.Pagination;
using XpertSphere.MonolithApi.Utils;
using System.Globalization;

namespace XpertSphere.MonolithApi.Services;

public class UserService(XpertSphereDbContext context) : IUserService
{
    private readonly XpertSphereDbContext _context = context;

    public async Task<User> Post(CreateUserDto createUserDto)
    {
        var user = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Email = createUserDto.Email,
            PhoneNumber = createUserDto.PhoneNumber,
            UserType = createUserDto.UserType,
            OrganizationId = createUserDto.OrganizationId,
            EmployeeId = createUserDto.EmployeeId,
            Department = createUserDto.Department,
            HireDate = createUserDto.HireDate,
            LinkedInProfile = createUserDto.LinkedInProfile,
            Skills = createUserDto.Skills,
            Experience = createUserDto.Experience,
            DesiredSalary = createUserDto.DesiredSalary,
            Availability = createUserDto.Availability,
            Address = createUserDto.Address != null
                ? new Address
                {
                    StreetNumber = createUserDto.Address.StreetNumber,
                    StreetName = createUserDto.Address.StreetName,
                    City = createUserDto.Address.City,
                    PostalCode = createUserDto.Address.PostalCode,
                    Region = createUserDto.Address.Region,
                    Country = createUserDto.Address.Country,
                    AddressLine2 = createUserDto.Address.AddressLine2
                }
                : new Address()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> Get(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user ?? throw new KeyNotFoundException(Constants.USER_NOT_FOUND);
    }

    public async Task<User?> GetByEmail(string email)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        return user ?? throw new KeyNotFoundException(Constants.USER_NOT_FOUND);
    }

    public async Task<ResponseResource<User>> GetAll(UserFilterDto userFilters)
    {
        IQueryable<User> source = _context.Users.AsNoTracking()
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        var filterdData = ApplyFilters(source, userFilters);

        return await PaginationService<User>.Paginate(userFilters.PageNumber, userFilters.PageSize, filterdData);
    }

    public async Task<User?> Put(Guid id,  User user)
    {
        if(user.Id != id) throw new InvalidDataException(Constants.INVALID_ID);

        if (await _context.Users.AnyAsync(u => u.Id == id)) throw new KeyNotFoundException(Constants.USER_NOT_FOUND);

        user.UpdatedAt = DateTime.UtcNow;

        _context.Entry(user).State = EntityState.Modified;
        _context.Entry(user).Property(a => a.CreatedAt).IsModified = false;
        _context.Entry(user).Property(a => a.Email).IsModified = false;

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Exists(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExists(string email, Guid? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Email == email);
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<User?> UpdateLastLogin(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return user;
    }


    private static IQueryable<User> ApplyFilters(IQueryable<User> query, UserFilterDto filter)
    {
        if (!string.IsNullOrEmpty(filter.SearchTerms))
        {
            query = query.Where(u =>
                u.FirstName.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                u.LastName.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                u.Email!.Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase) ||
                u.Address.ToString().Contains(filter.SearchTerms, StringComparison.CurrentCultureIgnoreCase)
            );
        }

        if (filter.UserType.HasValue)
        {
            query = query.Where(u => u.UserType == filter.UserType);
        }

        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == filter.OrganizationId);
        }

        if (!string.IsNullOrEmpty(filter.Department))
        {
            query = query.Where(u => u.Department == filter.Department);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive);
        }

        if (filter.MinExperience.HasValue)
        {
            query = query.Where(u => u.Experience >= filter.MinExperience);
        }

        if (filter.MaxExperience.HasValue)
        {
            query = query.Where(u => u.Experience <= filter.MaxExperience);
        }

        if (filter.MinSalary.HasValue)
        {
            query = query.Where(u => u.DesiredSalary >= filter.MinSalary);
        }

        if (filter.MaxSalary.HasValue)
        {
            query = query.Where(u => u.DesiredSalary <= filter.MaxSalary);
        }

        if (!string.IsNullOrEmpty(filter.Skills))
        {
            query = query.Where(u => u.Skills != null && u.Skills.Contains(filter.Skills));
        }

        var isDescending = filter.SortDirection == SortDirection.Descending;

        return filter.SortBy?.ToLower() switch
        {
            "firstname" => isDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "lastname" => isDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
            "email" => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "usertype" => isDescending ? query.OrderByDescending(u => u.UserType) : query.OrderBy(u => u.UserType),
            "createdat" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            "lastloginat" => isDescending
                ? query.OrderByDescending(u => u.LastLoginAt)
                : query.OrderBy(u => u.LastLoginAt),
            _ => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };
    }
}