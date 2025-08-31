using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Data;
using XpertSphere.MonolithApi.DTOs.RolePermission;
using XpertSphere.MonolithApi.Interfaces;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Utils.Results;

namespace XpertSphere.MonolithApi.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly XpertSphereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IValidator<AssignPermissionDto> _assignPermissionValidator;
    private readonly ILogger<RolePermissionService> _logger;

    public RolePermissionService(
        XpertSphereDbContext context,
        IMapper mapper,
        IValidator<AssignPermissionDto> assignPermissionValidator,
        ILogger<RolePermissionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _assignPermissionValidator = assignPermissionValidator;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<RolePermissionDto>>> GetRolePermissionsAsync(Guid roleId)
    {
        try
        {
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .OrderBy(rp => rp.Permission.Category)
                .ThenBy(rp => rp.Permission.Resource)
                .ThenBy(rp => rp.Permission.Action)
                .ToListAsync();

            var rolePermissionDtos = _mapper.Map<IEnumerable<RolePermissionDto>>(rolePermissions);
            return ServiceResult<IEnumerable<RolePermissionDto>>.Success(rolePermissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for role {RoleId}", roleId);
            return ServiceResult<IEnumerable<RolePermissionDto>>.InternalError("An error occurred while retrieving role permissions");
        }
    }

    public async Task<ServiceResult<IEnumerable<RolePermissionDto>>> GetPermissionRolesAsync(Guid permissionId)
    {
        try
        {
            var permissionRoles = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.PermissionId == permissionId)
                .OrderBy(rp => rp.Role.Name)
                .ToListAsync();

            var permissionRoleDtos = _mapper.Map<IEnumerable<RolePermissionDto>>(permissionRoles);
            return ServiceResult<IEnumerable<RolePermissionDto>>.Success(permissionRoleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for permission {PermissionId}", permissionId);
            return ServiceResult<IEnumerable<RolePermissionDto>>.InternalError("An error occurred while retrieving permission roles");
        }
    }

    public async Task<ServiceResult<RolePermissionDto>> AssignPermissionToRoleAsync(
        AssignPermissionDto assignPermissionDto)
    {
        try
        {
            var validationResult = await _assignPermissionValidator.ValidateAsync(assignPermissionDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<RolePermissionDto>.ValidationError(errors);
            }

            // Check if the role exists
            var role = await _context.Roles.FindAsync(assignPermissionDto.RoleId);
            if (role == null)
            {
                return ServiceResult<RolePermissionDto>.NotFound($"Role with ID {assignPermissionDto.RoleId} not found");
            }

            // Check if permission exists
            var permission = await _context.Permissions.FindAsync(assignPermissionDto.PermissionId);
            if (permission == null)
            {
                return ServiceResult<RolePermissionDto>.NotFound($"Permission with ID {assignPermissionDto.PermissionId} not found");
            }

            // Check if the role already has this permission
            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == assignPermissionDto.RoleId && 
                                          rp.PermissionId == assignPermissionDto.PermissionId);

            if (existingRolePermission != null)
            {
                return ServiceResult<RolePermissionDto>.Conflict("Role already has this permission assigned");
            }

            var rolePermission = _mapper.Map<RolePermission>(assignPermissionDto);
            rolePermission.Id = Guid.NewGuid();

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned permission {PermissionName} to role {RoleName}", permission.Name, role.Name);

            // Reload with includes for mapping
            var createdRolePermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.Id == rolePermission.Id);

            var rolePermissionDto = _mapper.Map<RolePermissionDto>(createdRolePermission);
            return ServiceResult<RolePermissionDto>.Success(rolePermissionDto, "Permission assigned to role successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", assignPermissionDto.PermissionId, assignPermissionDto.RoleId);
            return ServiceResult<RolePermissionDto>.InternalError("An error occurred while assigning permission to role");
        }
    }

    public async Task<ServiceResult> RemovePermissionFromRoleAsync(Guid rolePermissionId)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.Id == rolePermissionId);
                
            if (rolePermission == null)
            {
                return ServiceResult.NotFound($"Role permission assignment with ID {rolePermissionId} not found");
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed permission {PermissionName} from role {RoleName}", rolePermission.Permission.Name, rolePermission.Role.Name);
            return ServiceResult.Success("Permission removed from role successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role with ID {RolePermissionId}", rolePermissionId);
            return ServiceResult.InternalError("An error occurred while removing permission from role");
        }
    }

    public async Task<ServiceResult<bool>> RoleHasPermissionAsync(Guid roleId, string permissionName)
    {
        try
        {
            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp => rp.RoleId == roleId && 
                               rp.Permission.Name == permissionName);

            return ServiceResult<bool>.Success(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if role {RoleId} has permission {PermissionName}", roleId, permissionName);
            return ServiceResult<bool>.InternalError("An error occurred while checking role permission");
        }
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetRolePermissionNamesAsync(Guid roleId)
    {
        try
        {
            var permissionNames = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();

            return ServiceResult<IEnumerable<string>>.Success(permissionNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission names for role {RoleId}", roleId);
            return ServiceResult<IEnumerable<string>>.InternalError("An error occurred while retrieving role permission names");
        }
    }
}