using Microsoft.EntityFrameworkCore;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Application.Common.Interfaces;
using System.Text.Json;

namespace SmartPlanner.Infrastructure.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<Goal> Goals { get; set; } = null!;
    public virtual DbSet<GoalProgress> GoalProgresses { get; set; } = null!;
    public virtual DbSet<Challenge> Challenges { get; set; } = null!;
    public virtual DbSet<ChallengeParticipant> ChallengeParticipants { get; set; } = null!;
    public virtual DbSet<UserAchievement> UserAchievements { get; set; } = null!;
    public virtual DbSet<UserFriend> UserFriends { get; set; } = null!;
    public virtual DbSet<Achievement> Achievements { get; set; } = null!;
    public virtual DbSet<Role> Roles { get; set; } = null!;
    public virtual DbSet<UserRole> UserRoles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== ТОЛЬКО ДЛЯ НАВИГАЦИИ EF Core ==========

        // 1. Составные ключи (ОБЯЗАТЕЛЬНО)
        modelBuilder.Entity<UserFriend>()
            .HasKey(uf => new { uf.UserId, uf.FriendId });

        modelBuilder.Entity<UserAchievement>()
            .HasKey(ua => new { ua.UserId, ua.AchievementId });

        modelBuilder.Entity<ChallengeParticipant>()
            .HasKey(cp => new { cp.ChallengeId, cp.UserId });

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // 2. Навигационные свойства
        // User -> Goals (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Goals)
            .WithOne(g => g.User)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Goal -> GoalProgress (One-to-Many)
        modelBuilder.Entity<Goal>()
            .HasMany(g => g.ProgressHistory)
            .WithOne(gp => gp.Goal)
            .HasForeignKey(gp => gp.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // Challenge -> Creator (Many-to-One)
        modelBuilder.Entity<Challenge>()
            .HasOne(c => c.Creator)
            .WithMany(u => u.CreatedChallenges)
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Friends (Many-to-Many через UserFriend)
        modelBuilder.Entity<UserFriend>()
            .HasOne(uf => uf.User)
            .WithMany(u => u.Friends)
            .HasForeignKey(uf => uf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserFriend>()
            .HasOne(uf => uf.Friend)
            .WithMany()
            .HasForeignKey(uf => uf.FriendId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Achievements (Many-to-Many через UserAchievement)
        modelBuilder.Entity<UserAchievement>()
            .HasOne(ua => ua.User)
            .WithMany(u => u.Achievements)
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAchievement>()
            .HasOne(ua => ua.Achievement)
            .WithMany(a => a.UserAchievements)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Challenge -> Participants (Many-to-Many через ChallengeParticipant)
        modelBuilder.Entity<ChallengeParticipant>()
            .HasOne(cp => cp.Challenge)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChallengeParticipant>()
            .HasOne(cp => cp.User)
            .WithMany(u => u.ChallengeParticipants)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Roles (Many-to-Many через UserRole)
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. JSON конвертация для PostgreSQL jsonb
        modelBuilder.Entity<User>()
            .Property(u => u.Interests)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
            );

        // 4. Игнорировать вычисляемые свойства (их нет в БД)
        modelBuilder.Entity<Goal>().Ignore(g => g.GetProgressPercentage());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Автоматическое обновление UpdatedAt
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity &&
                       (e.State == EntityState.Modified || e.State == EntityState.Added));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Modified)
            {
                ((BaseEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
