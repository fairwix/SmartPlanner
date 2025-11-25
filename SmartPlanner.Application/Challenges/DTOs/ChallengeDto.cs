// SmartPlanner.Application/Challenges/Dtos/ChallengeDto.cs

using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Challenges.Dtos
{
    public class ChallengeDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsGroupChallenge { get; set; }
        public int TargetValue { get; set; }
        public int CurrentValue { get; set; }
        public double GroupProgressPercentage { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public List<ChallengeParticipantDto> Participants { get; set; } = new();
    }

    public class ChallengeParticipantDto : BaseDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PersonalContribution { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class CreateChallengeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsGroupChallenge { get; set; }
        public int TargetValue { get; set; }
        public Guid CreatedBy { get; set; }
    }
}