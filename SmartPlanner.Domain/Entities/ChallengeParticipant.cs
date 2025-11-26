using System;

namespace SmartPlanner.Domain.Entities;

    public class ChallengeParticipant : BaseEntity
    {
        public Guid ChallengeId { get; init; }
        public Guid UserId { get; init; }
        public ParticipantStatus Status { get; init; }
        public DateTime JoinedAt { get; init; }
        public int PersonalContribution { get; init; }

        public virtual Challenge Challenge { get; init; } = null!;
        public virtual User User { get; init; } = null!;
    }

