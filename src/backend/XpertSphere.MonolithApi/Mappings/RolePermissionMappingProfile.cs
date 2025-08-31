using AutoMapper;
using XpertSphere.MonolithApi.DTOs.RolePermission;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for RolePermission entity mappings
/// </summary>
public class RolePermissionMappingProfile : Profile
{
    public RolePermissionMappingProfile()
    {
        ConfigureRolePermissionMappings();
    }

    private void ConfigureRolePermissionMappings()
    {
        // RolePermission -> RolePermissionDto
        CreateMap<RolePermission, RolePermissionDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.Role.DisplayName))
            .ForMember(dest => dest.PermissionName, opt => opt.MapFrom(src => src.Permission.Name))
            .ForMember(dest => dest.PermissionResource, opt => opt.MapFrom(src => src.Permission.Resource))
            .ForMember(dest => dest.PermissionAction, opt => opt.MapFrom(src => src.Permission.Action.ToString()));

        // AssignPermissionDto -> RolePermission
        CreateMap<AssignPermissionDto, RolePermission>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Permission, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore());
    }
}