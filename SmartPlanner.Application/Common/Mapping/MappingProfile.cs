using AutoMapper;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Application.Users.Dtos;
using SmartPlanner.Application.Achievements.Dtos;
using SmartPlanner.Application.Challenges.Commands;
using SmartPlanner.Application.Challenges.Dtos;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Users.Commands;

namespace SmartPlanner.Application.Common.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ========== Goals Mapping ==========
        CreateMap<Goal, GoalDto>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
            .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.GetProgressPercentage()))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired()))
            .ForMember(dest => dest.IsOnTrack, opt => opt.MapFrom(src => src.IsOnTrack()));

        CreateMap<CreateGoalDto, CreateGoalCommand>();
        CreateMap<UpdateGoalDto, UpdateGoalCommand>();

        // ========== Users Mapping ==========
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, CreateUserCommand>();
        CreateMap<UpdateUserDto, UpdateUserCommand>();

        // ========== Achievements Mapping ==========
        CreateMap<Achievement, AchievementDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<UserAchievement, UserAchievementDto>()
            .ForMember(dest => dest.AchievementName, opt => opt.MapFrom(src => src.Achievement.Name))
            .ForMember(dest => dest.AchievementDescription, opt => opt.MapFrom(src => src.Achievement.Description))
            .ForMember(dest => dest.BadgeImage, opt => opt.MapFrom(src => src.Achievement.BadgeImage));

        // ========== Challenges Mapping ==========
        CreateMap<Challenge, ChallengeDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive()))
            .ForMember(dest => dest.GroupProgressPercentage, opt => opt.MapFrom(src => src.GetGroupProgressPercentage()));

        CreateMap<ChallengeParticipant, ChallengeParticipantDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // ========== Commands Mapping ==========
        CreateMap<CreateGoalCommand, Goal>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<CreateChallengeCommand, Challenge>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<ChallengeType>(src.Type)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.LastLogin, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.StreakCount, opt => opt.MapFrom(_ => 0));
    }
}
