
namespace SmartPlanner.Domain.Entities
{
    public class UserClaim : BaseEntity
    {
        public Guid UserId { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;

        public virtual User User { get; set; } = null!;
    }
}
