// SmartPlanner.Application/Common/Mapping/MappingProfile.cs
using AutoMapper;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Users.Commands;

namespace SmartPlanner.Application.Common.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Goals mapping
            CreateMap<Goal, GoalDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
                .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired()))
                .ForMember(dest => dest.IsOnTrack, opt => opt.MapFrom(src => src.IsOnTrack()));

            CreateMap<CreateGoalDto, CreateGoalCommand>();
            CreateMap<UpdateGoalDto, UpdateGoalCommand>();

            // Users mapping
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, CreateUserCommand>();
            CreateMap<UpdateUserDto, UpdateUserCommand>();

            // Achievements mapping
            CreateMap<Achievement, AchievementDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

            CreateMap<UserAchievement, UserAchievementDto>()
                .ForMember(dest => dest.AchievementName, opt => opt.MapFrom(src => src.Achievement.Name))
                .ForMember(dest => dest.AchievementDescription, opt => opt.MapFrom(src => src.Achievement.Description))
                .ForMember(dest => dest.BadgeImage, opt => opt.MapFrom(src => src.Achievement.BadgeImage));

            // Challenges mapping
            CreateMap<Challenge, ChallengeDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.GroupProgressPercentage, opt => opt.MapFrom(src => src.GroupProgressPercentage));

            CreateMap<ChallengeParticipant, ChallengeParticipantDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            // Commands mapping
            CreateMap<CreateChallengeCommand, Domain.Entities.Challenge>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<ChallengeType>(src.Type)));
        }
    }
}