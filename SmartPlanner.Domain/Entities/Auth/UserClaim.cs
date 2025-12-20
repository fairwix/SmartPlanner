
namespace SmartPlanner.Domain.Entities
{
    public class UserClaim : BaseEntity
    {
        public Guid UserId { get; set; }
        public string ClaimType { get; set; } = string.Empty;  // "SubscriptionLevel", "Department"
        public string ClaimValue { get; set; } = string.Empty; // "Premium", "Engineering"

        // Навигационное свойство
        public virtual User User { get; set; } = null!;
    }
}
