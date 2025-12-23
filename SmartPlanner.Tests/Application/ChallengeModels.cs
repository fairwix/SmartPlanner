// Tests/Unit/Application/ChallengeModels.cs
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.UnitTests
{
    // Временные модели для тестов, основанные на вашем коде
    public class TestChallenge
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ChallengeType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsGroupChallenge { get; set; }
        public int TargetValue { get; set; }
        public int CurrentValue { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public ICollection<TestChallengeParticipant> Participants { get; set; } = new List<TestChallengeParticipant>();
    }

    public class TestChallengeParticipant
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UserId { get; set; }
        public string? Username { get; set; } // Может быть nullable
        public ParticipantStatus Status { get; set; }
        public int PersonalContribution { get; set; }
        public DateTime JoinedAt { get; set; }
        public Guid ChallengeId { get; set; }
        public TestChallenge? Challenge { get; set; }
    }
}
