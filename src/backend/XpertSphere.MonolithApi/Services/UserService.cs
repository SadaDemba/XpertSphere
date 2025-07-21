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
using Microsoft.AspNetCore.Identity;

namespace XpertSphere.MonolithApi.Services;

public class UserService(XpertSphereDbContext context, UserManager<User> userManager, ILogger<UserService> logger) : IUserService
{
    private readonly XpertSphereDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<UserService> _logger = logger;

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
                    Street = createUserDto.Address.StreetName,
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

    public async Task<User?> Put(Guid id, UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                throw new KeyNotFoundException(Constants.USER_NOT_FOUND);
            }

            // Update properties
            user.FirstName = updateUserDto.FirstName ?? user.FirstName;
            user.LastName = updateUserDto.LastName ?? user.LastName;
            user.PhoneNumber = updateUserDto.PhoneNumber ?? user.PhoneNumber;
            user.UserType = updateUserDto.UserType ?? user.UserType;
            user.OrganizationId = updateUserDto.OrganizationId ?? user.OrganizationId;
            user.EmployeeId = updateUserDto.EmployeeId ?? user.EmployeeId;
            user.Department = updateUserDto.Department ?? user.Department;
            user.HireDate = updateUserDto.HireDate ?? user.HireDate;
            user.LinkedInProfile = updateUserDto.LinkedInProfile ?? user.LinkedInProfile;
            user.Skills = updateUserDto.Skills ?? user.Skills;
            user.Experience = updateUserDto.Experience ?? user.Experience;
            user.DesiredSalary = updateUserDto.DesiredSalary ?? user.DesiredSalary;
            user.Availability = updateUserDto.Availability ?? user.Availability;
            user.UpdatedAt = DateTime.UtcNow;

            // Update address if provided
            if (updateUserDto.Address != null)
            {
                user.Address.StreetNumber = updateUserDto.Address.StreetNumber ?? user.Address.StreetNumber;
                user.Address.Street = updateUserDto.Address.StreetName ?? user.Address.Street;
                user.Address.City = updateUserDto.Address.City ?? user.Address.City;
                user.Address.PostalCode = updateUserDto.Address.PostalCode ?? user.Address.PostalCode;
                user.Address.Region = updateUserDto.Address.Region ?? user.Address.Region;
                user.Address.Country = updateUserDto.Address.Country ?? user.Address.Country;
                user.Address.AddressLine2 = updateUserDto.Address.AddressLine2 ?? user.Address.AddressLine2;
            }

            // Recalculate profile completion
            user.CalculateProfileCompletion();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update user: {errors}");
            }

            _logger.LogInformation("User updated successfully: {UserId}", id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            throw;
        }
    }

    public async Task<bool> Exists(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExists(string email, Guid? excludeUserId = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        return excludeUserId == null || user.Id != excludeUserId;
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return false;
            }

            // Soft delete: mark as inactive instead of hard delete for audit purposes
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User deactivated successfully: {UserId}", id);
                return true;
            }

            _logger.LogWarning("Failed to deactivate user: {UserId}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", id);
            return false;
        }
    }

    public async Task<bool> HardDelete(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User permanently deleted: {UserId}", id);
                return true;
            }

            _logger.LogWarning("Failed to delete user: {UserId}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return false;
        }
    }

    public async Task<User?> UpdateLastLogin(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return null;

            user.UpdateLastLogin();
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Last login updated for user: {UserId}", id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", id);
            return null;
        }
    }

    public async Task<bool> ActivateUser(Guid id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User activated successfully: {UserId}", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user: {UserId}", id);
            return false;
        }
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