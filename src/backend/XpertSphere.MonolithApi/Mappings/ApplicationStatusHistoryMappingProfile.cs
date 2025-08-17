using AutoMapper;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

public class ApplicationStatusHistoryMappingProfile : Profile
{
    public ApplicationStatusHistoryMappingProfile()
    {
        ConfigureApplicationStatusHistoryMappings();
    }

    private void ConfigureApplicationStatusHistoryMappings()
    {
        // ApplicationStatusHistory -> ApplicationStatusHistoryDto
        CreateMap<ApplicationStatusHistory, ApplicationStatusHistoryDto>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.StatusDisplayName))
            .ForMember(dest => dest.RatingDescription, opt => opt.MapFrom(src => src.RatingDescription))
            .ForMember(dest => dest.HasRating, opt => opt.MapFrom(src => src.HasRating))
            .ForMember(dest => dest.UpdatedByUserName, opt => opt.MapFrom(src => src.UpdatedByUser.FullName));

        // AddStatusChangeDto -> ApplicationStatusHistory
        CreateMap<AddStatusChangeDto, ApplicationStatusHistory>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Application, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore());
    }
}