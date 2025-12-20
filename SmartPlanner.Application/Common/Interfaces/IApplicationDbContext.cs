using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Users
    DbSet<User> Users { get; }

    // Goals
    DbSet<Goal> Goals { get; }

    // Achievements
    DbSet<Achievement> Achievements { get; }
    DbSet<UserAchievement> UserAchievements { get; }

    // Challenges
    DbSet<Challenge> Challenges { get; }
    DbSet<ChallengeParticipant> ChallengeParticipants { get; }

    // Social relations
    DbSet<UserFriend> UserFriends { get; }

    // Progress and roles
    DbSet<GoalProgress> GoalProgresses { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }

    DbSet<Interest> Interests { get; }
    DbSet<UserInterest> UserInterests { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserClaim> UserClaims { get; }

    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; }
    DbSet<SecurityAuditLog> SecurityAuditLogs { get; }

    // Метод для сохранения изменений
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
