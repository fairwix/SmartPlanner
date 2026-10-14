using SmartPlanner.Domain.Entities;

public class ChallengeParticipant
{
    public Guid ChallengeId { get; init; }
    public Guid UserId { get; init; }
    public ParticipantStatus Status { get; set; }
    public DateTime JoinedAt { get; init; }
    public int PersonalContribution { get; set; }

    public virtual Challenge Challenge { get; init; } = null!;
    public virtual User User { get; init; } = null!;
}
