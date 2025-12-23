namespace SmartPlanner.Domain.Entities;

public class UserInterest : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid InterestId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Interest Interest { get; set; } = null!;
}
