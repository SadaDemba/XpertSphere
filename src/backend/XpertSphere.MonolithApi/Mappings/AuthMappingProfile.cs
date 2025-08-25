using AutoMapper;
using XpertSphere.MonolithApi.DTOs.Auth;
using XpertSphere.MonolithApi.DTOs.User;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.Mappings;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, AuthResponseDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.MapFrom(src => src.RefreshToken))
            .ForMember(dest => dest.TokenExpiry, opt => opt.MapFrom(src => src.RefreshTokenExpiry))
            .ForMember(dest => dest.EmailConfirmationToken, opt => opt.Ignore())
            .ForMember(dest => dest.RedirectUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => new List<string>()))
            .ForMember(dest => dest.RequiresEntraId, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.EntraIdAuthUrl, opt => opt.Ignore())
            .ForMember(dest => dest.AuthType, opt => opt.MapFrom(src => "Local"))
            .ForMember(dest => dest.EntraIdState, opt => opt.Ignore())
            .ForMember(dest => dest.IsExternalAuth, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ExternalId)))
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.ExternalId))
            .ForMember(dest => dest.EntraIdClaims, opt => opt.Ignore());
    }
}