using AutoMapper;
using XpertSphere.MonolithApi.DTOs.UserRole;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for UserRole entity mappings
/// </summary>
public class UserRoleMappingProfile : Profile
{
    public UserRoleMappingProfile()
    {
        ConfigureUserRoleMappings();
    }

    private void ConfigureUserRoleMappings()
    {
        // UserRole -> UserRoleDto
        CreateMap<UserRole, UserRoleDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.Role.DisplayName))
            .ForMember(dest => dest.AssignedByName, opt => opt.MapFrom(src => 
                src.AssignedByUser != null ? $"{src.AssignedByUser.FirstName} {src.AssignedByUser.LastName}" : null));

        // AssignRoleDto -> UserRole
        CreateMap<AssignRoleDto, UserRole>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore());
    }
}