// SmartPlanner.Domain/Entities/UserInterest.cs
namespace SmartPlanner.Domain.Entities;

public class UserInterest : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid InterestId { get; set; }

    // Навигационные свойства
    public virtual User User { get; set; } = null!;
    public virtual Interest Interest { get; set; } = null!;
}
