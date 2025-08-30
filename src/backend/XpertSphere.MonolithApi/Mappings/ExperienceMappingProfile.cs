using AutoMapper;
using XpertSphere.MonolithApi.DTOs.ExperienceDtos;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for Experience entity mappings
/// </summary>
public class ExperienceMappingProfile : Profile
{
    public ExperienceMappingProfile()
    {
        ConfigureExperienceMappings();
    }

    private void ConfigureExperienceMappings()
    {
        // Experience -> ExperienceDto
        CreateMap<Experience, ExperienceDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

        // CreateExperienceDto -> Experience
        CreateMap<CreateExperienceDto, Experience>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // UpdateExperienceDto -> Experience
        CreateMap<UpdateExperienceDto, Experience>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}