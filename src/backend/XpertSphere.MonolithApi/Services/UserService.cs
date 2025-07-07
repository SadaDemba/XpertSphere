using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Services;

public class UserService : IUserService
{
    private readonly XpertSphereDbContext _context;

    public UserService(XpertSphereDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
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

        return await GetByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to retrieve created user");
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        return user != null ? MapToDto(user) : null;
    }

    public async Task<(List<UserDto> Users, int TotalCount)> GetAllAsync(UserFilterDto filter)
    {
        var query = _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Apply filters
        query = ApplyFilters(query, filter);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        // Apply pagination
        var users = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (users.Select(MapToDto).ToList(), totalCount);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateUserDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        // Update only provided fields
        if (updateUserDto.FirstName != null) user.FirstName = updateUserDto.FirstName;
        if (updateUserDto.LastName != null) user.LastName = updateUserDto.LastName;
        if (updateUserDto.Email != null) user.Email = updateUserDto.Email;
        if (updateUserDto.PhoneNumber != null) user.PhoneNumber = updateUserDto.PhoneNumber;
        if (updateUserDto.OrganizationId.HasValue) user.OrganizationId = updateUserDto.OrganizationId;
        if (updateUserDto.EmployeeId != null) user.EmployeeId = updateUserDto.EmployeeId;
        if (updateUserDto.Department != null) user.Department = updateUserDto.Department;
        if (updateUserDto.HireDate.HasValue) user.HireDate = updateUserDto.HireDate;
        if (updateUserDto.LinkedInProfile != null) user.LinkedInProfile = updateUserDto.LinkedInProfile;
        if (updateUserDto.Skills != null) user.Skills = updateUserDto.Skills;
        if (updateUserDto.Experience.HasValue) user.Experience = updateUserDto.Experience;
        if (updateUserDto.DesiredSalary.HasValue) user.DesiredSalary = updateUserDto.DesiredSalary;
        if (updateUserDto.Availability.HasValue) user.Availability = updateUserDto.Availability;
        if (updateUserDto.IsActive.HasValue) user.IsActive = updateUserDto.IsActive.Value;

        // Update address if provided
        if (updateUserDto.Address != null)
        {
            user.Address.StreetNumber = updateUserDto.Address.StreetNumber;
            user.Address.StreetName = updateUserDto.Address.StreetName;
            user.Address.City = updateUserDto.Address.City;
            user.Address.PostalCode = updateUserDto.Address.PostalCode;
            user.Address.Region = updateUserDto.Address.Region;
            user.Address.Country = updateUserDto.Address.Country;
            user.Address.AddressLine2 = updateUserDto.Address.AddressLine2;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Email == email);
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<UserDto?> UpdateLastLoginAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<List<UserDto>> GetByOrganizationAsync(Guid organizationId)
    {
        var users = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.OrganizationId == organizationId)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<List<UserDto>> GetCandidatesAsync(UserFilterDto filter)
    {
        filter.UserType = UserType.External;
        var (users, _) = await GetAllAsync(filter);
        return users;
    }

    public async Task<List<UserDto>> GetInternalUsersAsync(UserFilterDto filter)
    {
        filter.UserType = UserType.Internal;
        var (users, _) = await GetAllAsync(filter);
        return users;
    }

    private static IQueryable<User> ApplyFilters(IQueryable<User> query, UserFilterDto filter)
    {
        if (!string.IsNullOrEmpty(filter.Search))
        {
            query = query.Where(u =>
                u.FirstName.Contains(filter.Search) ||
                u.LastName.Contains(filter.Search) ||
                u.Email.Contains(filter.Search));
        }

        if (filter.UserType.HasValue)
        {
            query = query.Where(u => u.UserType == filter.UserType.Value);
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

        return query;
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
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

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            UserType = user.UserType,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name,
            EmployeeId = user.EmployeeId,
            Department = user.Department,
            HireDate = user.HireDate,
            LinkedInProfile = user.LinkedInProfile,
            Skills = user.Skills,
            Experience = user.Experience,
            DesiredSalary = user.DesiredSalary,
            Availability = user.Availability,
            Address = new AddressDto
            {
                StreetNumber = user.Address.StreetNumber,
                StreetName = user.Address.StreetName,
                City = user.Address.City,
                PostalCode = user.Address.PostalCode,
                Region = user.Address.Region,
                Country = user.Address.Country,
                AddressLine2 = user.Address.AddressLine2
            },
            UserRoles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                RoleDisplayName = ur.Role.DisplayName,
                AssignedAt = ur.AssignedAt,
                ExpiresAt = ur.ExpiresAt,
                IsActive = ur.IsActive
            }).ToList()
        };
    }
}