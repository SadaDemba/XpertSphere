using AutoMapper;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Models;
using XpertSphere.MonolithApi.Models.Base;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for User entity mappings
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        ConfigureUserMappings();
        ConfigureAddressMappings();
    }

    private void ConfigureUserMappings()
    {
        // User -> UserDto
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.Name : null))
            .ForMember(dest => dest.Experiences, opt => opt.MapFrom(src => src.Experiences.ToList()))
            .ForMember(dest => dest.Trainings, opt => opt.MapFrom(src => src.Trainings.ToList()))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name).ToList()))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));

        // User -> UserProfileDto
        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.Name : null))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name).ToList()));

        // User -> UserSearchResultDto
        CreateMap<User, UserSearchResultDto>()
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.Organization != null ? src.Organization.Name : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address != null ? src.Address.City : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address != null ? src.Address.Country : null));

        // CreateUserDto -> User
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Organization, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.Experiences, opt => opt.Ignore())
            .ForMember(dest => dest.Trainings, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.ProfileCompletionPercentage, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.EmailNotificationsEnabled, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.SmsNotificationsEnabled, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.PreferredLanguage, opt => opt.MapFrom(src => "en"))
            .ForMember(dest => dest.TimeZone, opt => opt.MapFrom(src => "UTC"));

        // UpdateUserDto -> User (for partial updates)
        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Email)))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Organization, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.Experiences, opt => opt.Ignore())
            .ForMember(dest => dest.Trainings, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private void ConfigureAddressMappings()
    {
        // Address -> AddressDto
        CreateMap<Address, AddressDto>().ReverseMap();
    }
}
