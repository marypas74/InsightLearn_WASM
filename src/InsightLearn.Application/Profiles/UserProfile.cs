using AutoMapper;
using InsightLearn.Core.Entities;
using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Profiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles are set separately in service

        CreateMap<RegisterDto, User>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DateJoined, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<UpdateProfileDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.Ignore());
    }
}