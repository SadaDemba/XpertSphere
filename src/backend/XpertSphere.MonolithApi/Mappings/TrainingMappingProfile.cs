using AutoMapper;
using XpertSphere.MonolithApi.DTOs.TrainingDtos;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

/// <summary>
/// AutoMapper profile for Training entity mappings
/// </summary>
public class TrainingMappingProfile : Profile
{
    public TrainingMappingProfile()
    {
        ConfigureTrainingMappings();
    }

    private void ConfigureTrainingMappings()
    {
        // Training -> TrainingDto
        CreateMap<Training, TrainingDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

        // CreateTrainingDto -> Training
        CreateMap<CreateTrainingDto, Training>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // UpdateTrainingDto -> Training
        CreateMap<UpdateTrainingDto, Training>()
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