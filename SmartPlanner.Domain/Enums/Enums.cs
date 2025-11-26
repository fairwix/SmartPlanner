namespace SmartPlanner.Domain.Entities;

    public enum GoalCategory
    {
        Sports,
        Education,
        Health,
        Career,
        Finance,
        Personal,
        Social,
        Hobbies
    }

    public enum GoalPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AchievementType
    {
        Streak,
        GoalsCompleted,
        Friends,
        ChallengeCompletion,
        Social
    }

    public enum FriendStatus
    {
        Pending,
        Accepted,
        Blocked
    }

    public enum ChallengeType
    {
        StepCount,
        Reading,
        Exercise,
        Learning,
        Custom
    }

    public enum ParticipantStatus
    {
        Invited,
        Joined,
        Completed,
        Left
    }

