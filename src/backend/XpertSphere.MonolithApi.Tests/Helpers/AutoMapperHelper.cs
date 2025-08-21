using AutoMapper;
using Moq;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.Role;
using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.DTOs.Permission;
using XpertSphere.MonolithApi.DTOs.RolePermission;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.Tests.Helpers;

public static class AutoMapperHelper
{
    public static IMapper CreateMapper()
    {
        // Return a simple mock mapper for tests
        var mockMapper = new Mock<IMapper>();
        
        // Setup basic mappings that tests expect
        mockMapper.Setup(m => m.Map<User>(It.IsAny<RegisterDto>()))
            .Returns((RegisterDto dto) => new User 
            { 
                Email = dto.Email, 
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            });

        // Role mappings
        mockMapper.Setup(m => m.Map<Role>(It.IsAny<CreateRoleDto>()))
            .Returns((CreateRoleDto dto) => new Role
            {
                Name = dto.Name,
                DisplayName = dto.DisplayName,
                Description = dto.Description
            });

        mockMapper.Setup(m => m.Map<RoleDto>(It.IsAny<Role>()))
            .Returns((Role role) => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                UsersCount = 0,
                PermissionsCount = 0
            });

        mockMapper.Setup(m => m.Map(It.IsAny<UpdateRoleDto>(), It.IsAny<Role>()))
            .Callback((UpdateRoleDto dto, Role role) =>
            {
                role.DisplayName = dto.DisplayName;
                role.Description = dto.Description;
                role.IsActive = dto.IsActive;
            });

        // UserRole mappings
        mockMapper.Setup(m => m.Map<UserRole>(It.IsAny<AssignRoleDto>()))
            .Returns((AssignRoleDto dto) => new UserRole
            {
                UserId = dto.UserId,
                RoleId = dto.RoleId,
                ExpiresAt = dto.ExpiresAt
            });

        mockMapper.Setup(m => m.Map<UserRoleDto>(It.IsAny<UserRole>()))
            .Returns((UserRole userRole) => new UserRoleDto
            {
                Id = userRole.Id,
                UserId = userRole.UserId,
                RoleId = userRole.RoleId,
                UserFullName = "Test User",
                UserEmail = "test@example.com",
                RoleName = "TestRole",
                RoleDisplayName = "Test Role",
                AssignedAt = userRole.AssignedAt,
                AssignedBy = userRole.AssignedBy,
                AssignedByName = "Admin User",
                IsActive = userRole.IsActive,
                ExpiresAt = userRole.ExpiresAt
            });

        mockMapper.Setup(m => m.Map<IEnumerable<UserRoleDto>>(It.IsAny<IEnumerable<UserRole>>()))
            .Returns((IEnumerable<UserRole> userRoles) => userRoles.Select(ur => new UserRoleDto
            {
                Id = ur.Id,
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                UserFullName = "Test User",
                UserEmail = "test@example.com",
                RoleName = "TestRole",
                RoleDisplayName = "Test Role",
                AssignedAt = ur.AssignedAt,
                AssignedBy = ur.AssignedBy,
                AssignedByName = "Admin User",
                IsActive = ur.IsActive,
                ExpiresAt = ur.ExpiresAt
            }));

        // Permission mappings
        mockMapper.Setup(m => m.Map<Permission>(It.IsAny<CreatePermissionDto>()))
            .Returns((CreatePermissionDto dto) => new Permission
            {
                Name = dto.Name,
                Resource = dto.Resource,
                Action = dto.Action,
                Scope = dto.Scope,
                Category = dto.Category,
                Description = dto.Description
            });

        mockMapper.Setup(m => m.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns((Permission permission) => new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Resource = permission.Resource,
                Action = permission.Action,
                Scope = permission.Scope,
                Category = permission.Category,
                Description = permission.Description,
                CreatedAt = permission.CreatedAt,
                UpdatedAt = permission.UpdatedAt,
                RolesCount = 0
            });

        mockMapper.Setup(m => m.Map<IEnumerable<PermissionDto>>(It.IsAny<IEnumerable<Permission>>()))
            .Returns((IEnumerable<Permission> permissions) => permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Resource = p.Resource,
                Action = p.Action,
                Scope = p.Scope,
                Category = p.Category,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                RolesCount = 0
            }));

        // RolePermission mappings
        mockMapper.Setup(m => m.Map<RolePermission>(It.IsAny<AssignPermissionDto>()))
            .Returns((AssignPermissionDto dto) => new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId
            });

        mockMapper.Setup(m => m.Map<RolePermissionDto>(It.IsAny<RolePermission>()))
            .Returns((RolePermission rp) => new RolePermissionDto
            {
                Id = rp.Id,
                RoleId = rp.RoleId,
                PermissionId = rp.PermissionId,
                RoleName = "TestRole",
                RoleDisplayName = "Test Role",
                PermissionName = "TestPermission",
                PermissionResource = "User",
                PermissionAction = "Read",
                CreatedAt = rp.CreatedAt
            });

        mockMapper.Setup(m => m.Map<IEnumerable<RolePermissionDto>>(It.IsAny<IEnumerable<RolePermission>>()))
            .Returns((IEnumerable<RolePermission> rolePermissions) => rolePermissions.Select(rp => new RolePermissionDto
            {
                Id = rp.Id,
                RoleId = rp.RoleId,
                PermissionId = rp.PermissionId,
                RoleName = "TestRole",
                RoleDisplayName = "Test Role",
                PermissionName = "TestPermission",
                PermissionResource = "User",
                PermissionAction = "Read",
                CreatedAt = rp.CreatedAt
            }));

        // Organization mappings
        mockMapper.Setup(m => m.Map<Organization>(It.IsAny<CreateOrganizationDto>()))
            .Returns((CreateOrganizationDto dto) => new Organization
            {
                Name = dto.Name,
                Code = dto.Code,
                Industry = dto.Industry,
                Size = dto.Size,
                Address = dto.Address,
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                Website = dto.Website
            });

        mockMapper.Setup(m => m.Map<OrganizationDto>(It.IsAny<Organization>()))
            .Returns((Organization org) => new OrganizationDto
            {
                Id = org.Id,
                Name = org.Name,
                Code = org.Code,
                Description = null, // Organization model doesn't have Description
                Industry = org.Industry,
                Size = org.Size ?? OrganizationSize.Small,
                Address = org.Address,
                ContactEmail = org.ContactEmail,
                ContactPhone = org.ContactPhone,
                Website = org.Website,
                IsActive = org.IsActive,
                CreatedAt = org.CreatedAt,
                UpdatedAt = org.UpdatedAt,
                UsersCount = 0
            });

        mockMapper.Setup(m => m.Map<IEnumerable<OrganizationDto>>(It.IsAny<IEnumerable<Organization>>()))
            .Returns((IEnumerable<Organization> organizations) => organizations.Select(org => new OrganizationDto
            {
                Id = org.Id,
                Name = org.Name,
                Code = org.Code,
                Description = null, // Organization model doesn't have Description
                Industry = org.Industry,
                Size = org.Size ?? OrganizationSize.Small,
                Address = org.Address,
                ContactEmail = org.ContactEmail,
                ContactPhone = org.ContactPhone,
                Website = org.Website,
                IsActive = org.IsActive,
                CreatedAt = org.CreatedAt,
                UpdatedAt = org.UpdatedAt,
                UsersCount = 0
            }));
            
        return mockMapper.Object;
    }
}