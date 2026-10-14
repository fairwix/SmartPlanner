using System;

namespace SmartPlanner.Domain.Entities;

    public class UserFriend
    {
        public Guid UserId { get; init; }
        public Guid FriendId { get; init; }
        public FriendStatus Status { get; init; }

        public virtual User User { get; init; } = null!;
        public virtual User Friend { get; init; } = null!;
    }

