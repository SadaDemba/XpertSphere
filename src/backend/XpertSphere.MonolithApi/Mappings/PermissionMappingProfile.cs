using AutoMapper;
using XpertSphere.MonolithApi.DTOs.Permission;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for Permission entity mappings
/// </summary>
public class PermissionMappingProfile : Profile
{
    public PermissionMappingProfile()
    {
        ConfigurePermissionMappings();
    }

    private void ConfigurePermissionMappings()
    {
        // Permission -> PermissionDto
        CreateMap<Permission, PermissionDto>()
            .ForMember(dest => dest.RolesCount, opt => opt.MapFrom(src => src.RolePermissions.Count));

        // CreatePermissionDto -> Permission
        CreateMap<CreatePermissionDto, Permission>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());
    }
}