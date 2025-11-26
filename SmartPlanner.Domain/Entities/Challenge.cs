using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPlanner.Domain.Entities;

    public class Challenge : BaseEntity
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ChallengeType Type { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public bool IsGroupChallenge { get; init; }
        public int TargetValue { get; init; }
        public int CurrentValue { get; set; }
        public Guid CreatedBy { get; init; }

        public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
        public double GroupProgressPercentage => TargetValue > 0 ? (CurrentValue * 100.0) / TargetValue : 0;

        public virtual User Creator { get; init; } = null!;
        public virtual List<ChallengeParticipant> Participants { get; set; } = new List<ChallengeParticipant>();

        public bool CanUserJoin(Guid userId)
        {
            return IsActive &&
                   !Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Joined);
        }

        public void AddParticipant(Guid userId)
        {
            if (CanUserJoin(userId))
            {
                Participants.Add(new ChallengeParticipant
                {
                    Id = Guid.NewGuid(),
                    ChallengeId = this.Id,
                    UserId = userId,
                    Status = ParticipantStatus.Joined,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        public bool IsExpired() => EndDate < DateTime.UtcNow;
    }

