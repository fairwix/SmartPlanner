using System;

namespace SmartPlanner.Domain.Entities
{
    public class UserFriend : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid FriendId { get; set; }
        public FriendStatus Status { get; set; }
        
        public virtual User User { get; set; } = null!;
        public virtual User Friend { get; set; } = null!;
    }
}