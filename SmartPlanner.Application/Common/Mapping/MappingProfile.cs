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
        CreateMap<Goal, GoalDto>()
            .ConstructUsing(src => new GoalDto(
                src.Id, src.CreatedAt, src.UpdatedAt, src.Title, src.Description,
                src.Category.ToString(), src.Priority.ToString(), src.DueDate,
                src.TargetValue, src.CurrentValue, src.GetProgressPercentage(),
                src.IsCompleted, src.IsAiGenerated, src.RewardAmount, src.UserId,
                src.IsExpired(), src.IsOnTrack()));

        CreateMap<CreateGoalDto, CreateGoalCommand>();
        CreateMap<UpdateGoalDto, UpdateGoalCommand>();

        CreateMap<User, UserDto>().ConstructUsing(src => new UserDto(
            src.Id,
            src.CreatedAt,
            src.UpdatedAt,
            src.Username,
            src.Email,
            (src.UserInterests ?? new List<UserInterest>())
                .Where(ui => ui.Interest != null)
                .Select(ui => ui.Interest!.Name)
                .ToList(),
            src.Balance,
            src.StreakCount,
            src.LastLoginAt));
        CreateMap<CreateUserDto, CreateUserCommand>();
        CreateMap<UpdateUserDto, UpdateUserCommand>();

        CreateMap<Achievement, AchievementDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<UserAchievement, UserAchievementDto>()
            .ForMember(dest => dest.AchievementName, opt => opt.MapFrom(src => src.Achievement.Name))
            .ForMember(dest => dest.AchievementDescription, opt => opt.MapFrom(src => src.Achievement.Description))
            .ForMember(dest => dest.BadgeImage, opt => opt.MapFrom(src => src.Achievement.BadgeImage));

        CreateMap<Challenge, ChallengeDto>()
            .ConstructUsing(src => new ChallengeDto(
                src.Id, src.CreatedAt, src.UpdatedAt, src.Title, src.Description,
                src.Type.ToString(), src.StartDate, src.EndDate, src.IsGroupChallenge,
                src.TargetValue, src.CurrentValue, src.GetGroupProgressPercentage(),
                src.IsActive(), src.CreatedBy,
                src.Participants.Select(participant => new ChallengeParticipantDto(
                    participant.UserId, participant.JoinedAt, participant.JoinedAt,
                    participant.UserId, participant.User == null ? string.Empty : participant.User.Username,
                    participant.Status.ToString(), participant.PersonalContribution,
                    participant.JoinedAt)).ToList()));

        CreateMap<ChallengeParticipant, ChallengeParticipantDto>()
            .ConstructUsing(src => new ChallengeParticipantDto(
                src.UserId, src.JoinedAt, src.JoinedAt, src.UserId,
                src.User == null ? string.Empty : src.User.Username, src.Status.ToString(),
                src.PersonalContribution, src.JoinedAt));

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
            .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.StreakCount, opt => opt.MapFrom(_ => 0));
    }
}
