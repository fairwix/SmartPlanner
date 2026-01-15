using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Goal> Goals { get; }

    DbSet<Achievement> Achievements { get; }
    DbSet<UserAchievement> UserAchievements { get; }

    DbSet<Challenge> Challenges { get; }
    DbSet<ChallengeParticipant> ChallengeParticipants { get; }

    DbSet<UserFriend> UserFriends { get; }

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

    DbSet<FileMetadata> FileMetadata { get; }
    DbSet<Message> Messages { get; }
    DbSet<MessageAttachment> MessageAttachments { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostAttachment> PostAttachments { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<UploadProgress> UploadProgresses { get; set; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
