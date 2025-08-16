using AutoMapper;
using XpertSphere.MonolithApi.DTOs.Organization;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for Organization entity mappings
/// </summary>
public class OrganizationMappingProfile : Profile
{
    public OrganizationMappingProfile()
    {
        ConfigureOrganizationMappings();
    }

    private void ConfigureOrganizationMappings()
    {
        // Organization -> OrganizationDto
        CreateMap<Organization, OrganizationDto>()
            .ForMember(dest => dest.UsersCount, opt => opt.MapFrom(src => src.Users.Count));

        // CreateOrganizationDto -> Organization
        CreateMap<CreateOrganizationDto, Organization>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore());

        // UpdateOrganizationDto -> Organization (for partial updates)
        CreateMap<UpdateOrganizationDto, Organization>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}