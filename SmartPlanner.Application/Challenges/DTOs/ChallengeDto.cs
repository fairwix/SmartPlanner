// SmartPlanner.Application/Challenges/Dtos/ChallengeDto.cs

using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Challenges.Dtos;

    public record ChallengeDto(
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Title,
        string Description,
        string Type,
        DateTime StartDate,
        DateTime EndDate,
        bool IsGroupChallenge,
        int TargetValue,
        int CurrentValue,
        double GroupProgressPercentage,
        bool IsActive,
        Guid CreatedBy,
        List<ChallengeParticipantDto> Participants) : BaseDto(Id, CreatedAt, UpdatedAt);

    public record ChallengeParticipantDto(
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid UserId,
        string Username,
        string Status,
        int PersonalContribution,
        DateTime JoinedAt) : BaseDto(Id, CreatedAt, UpdatedAt);

    public record CreateChallengeDto(
        string Title,
        string Description,
        string Type,
        DateTime StartDate,
        DateTime EndDate,
        bool IsGroupChallenge,
        int TargetValue,
        Guid CreatedBy);
