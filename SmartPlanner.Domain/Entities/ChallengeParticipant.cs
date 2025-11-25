using System;

namespace SmartPlanner.Domain.Entities
{
    public class ChallengeParticipant : BaseEntity
    {
        public Guid ChallengeId { get; set; }
        public Guid UserId { get; set; }
        public ParticipantStatus Status { get; set; }
        public DateTime JoinedAt { get; set; }
        public int PersonalContribution { get; set; }
        
        public virtual Challenge Challenge { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}