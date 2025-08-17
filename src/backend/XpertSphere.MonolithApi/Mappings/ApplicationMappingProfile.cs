using AutoMapper;
using XpertSphere.MonolithApi.DTOs.Application;
using XpertSphere.MonolithApi.DTOs.ApplicationStatusHistory;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        ConfigureApplicationMappings();
        ConfigureApplicationStatusHistoryMappings();
    }

    private void ConfigureApplicationMappings()
    {
        // Application -> ApplicationDto
        CreateMap<Application, ApplicationDto>()
            .ForMember(dest => dest.JobOfferTitle, opt => opt.MapFrom(src => src.JobOffer.Title))
            .ForMember(dest => dest.OrganizationName, opt => opt.MapFrom(src => src.JobOffer.Organization.Name))
            .ForMember(dest => dest.CandidateName, opt => opt.MapFrom(src => src.Candidate.FullName))
            .ForMember(dest => dest.CandidateEmail, opt => opt.MapFrom(src => src.Candidate.Email))
            .ForMember(dest => dest.AssignedTechnicalEvaluatorName, opt => opt.MapFrom(src => src.AssignedTechnicalEvaluator != null ? src.AssignedTechnicalEvaluator.FullName : null))
            .ForMember(dest => dest.AssignedManagerName, opt => opt.MapFrom(src => src.AssignedManager != null ? src.AssignedManager.FullName : null))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.StatusDisplayName))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.IsInProgress, opt => opt.MapFrom(src => src.IsInProgress))
            .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.StatusHistory.OrderBy(h => h.UpdatedAt)));

        // CreateApplicationDto -> Application
        CreateMap<CreateApplicationDto, Application>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentStatus, opt => opt.Ignore())
            .ForMember(dest => dest.Rating, opt => opt.Ignore())
            .ForMember(dest => dest.AppliedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.CandidateId, opt => opt.Ignore())
            .ForMember(dest => dest.JobOffer, opt => opt.Ignore())
            .ForMember(dest => dest.Candidate, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTechnicalEvaluator, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedManager, opt => opt.Ignore())
            .ForMember(dest => dest.StatusHistory, opt => opt.Ignore());

        // UpdateApplicationDto -> Application
        CreateMap<UpdateApplicationDto, Application>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentStatus, opt => opt.Ignore())
            .ForMember(dest => dest.AppliedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.JobOfferId, opt => opt.Ignore())
            .ForMember(dest => dest.CandidateId, opt => opt.Ignore())
            .ForMember(dest => dest.JobOffer, opt => opt.Ignore())
            .ForMember(dest => dest.Candidate, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTechnicalEvaluator, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedManager, opt => opt.Ignore())
            .ForMember(dest => dest.StatusHistory, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private void ConfigureApplicationStatusHistoryMappings()
    {
        // ApplicationStatusHistory -> ApplicationStatusHistoryDto
        CreateMap<ApplicationStatusHistory, ApplicationStatusHistoryDto>()
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.StatusDisplayName))
            .ForMember(dest => dest.RatingDescription, opt => opt.MapFrom(src => src.RatingDescription))
            .ForMember(dest => dest.HasRating, opt => opt.MapFrom(src => src.HasRating))
            .ForMember(dest => dest.UpdatedByUserName, opt => opt.MapFrom(src => src.UpdatedByUser.FullName));
    }
}