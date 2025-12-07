using System.Data;
using FluentMigrator;

[Migration(0001)]
public class InitialMigration : Migration
{
    public override void Up()
    {
        // Users table
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Username").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("PasswordHash").AsString().NotNullable()
            .WithColumn("Balance").AsInt32().WithDefaultValue(0)
            .WithColumn("StreakCount").AsInt32().WithDefaultValue(0)
            .WithColumn("LastLogin").AsDateTime().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // Goals table
        Create.Table("Goals")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Title").AsString(500).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Category").AsInt32().NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable()
            .WithColumn("DueDate").AsDateTime().NotNullable()
            .WithColumn("TargetValue").AsInt32().WithDefaultValue(1)
            .WithColumn("CurrentValue").AsInt32().WithDefaultValue(0)
            .WithColumn("IsCompleted").AsBoolean().WithDefaultValue(false)
            .WithColumn("IsAiGenerated").AsBoolean().WithDefaultValue(false)
            .WithColumn("RewardAmount").AsInt32().WithDefaultValue(10)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // GoalProgress table
        Create.Table("GoalProgress")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("GoalId").AsGuid().NotNullable()
            .WithColumn("Value").AsInt32().NotNullable()
            .WithColumn("PreviousValue").AsInt32().NotNullable()
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // Achievements table
        Create.Table("Achievements")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("BadgeImage").AsString(500).Nullable()
            .WithColumn("RewardAmount").AsInt32().WithDefaultValue(0)
            .WithColumn("Type").AsInt32().NotNullable()
            .WithColumn("Condition").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // Challenges table
        Create.Table("Challenges")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("Type").AsInt32().NotNullable()
            .WithColumn("StartDate").AsDateTime().NotNullable()
            .WithColumn("EndDate").AsDateTime().NotNullable()
            .WithColumn("IsGroupChallenge").AsBoolean().WithDefaultValue(false)
            .WithColumn("TargetValue").AsInt32().WithDefaultValue(0)
            .WithColumn("CurrentValue").AsInt32().WithDefaultValue(0)
            .WithColumn("CreatedBy").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // UserAchievements junction table - БЕЗ отдельного Id!
        Create.Table("UserAchievements")
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("AchievementId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("AwardedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ChallengeParticipants junction table - БЕЗ отдельного Id!
        Create.Table("ChallengeParticipants")
            .WithColumn("ChallengeId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Status").AsInt32().WithDefaultValue(1) // 1 = Joined
            .WithColumn("JoinedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("PersonalContribution").AsInt32().WithDefaultValue(0);

        // UserFriends junction table
        Create.Table("UserFriends")
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("FriendId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // Roles table
        Create.Table("Roles")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("NormalizedName").AsString(50).NotNullable();

        // UserRoles junction table
        Create.Table("UserRoles")
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("RoleId").AsInt32().NotNullable().PrimaryKey();

        // Foreign keys
        Create.ForeignKey("FK_Goals_Users")
            .FromTable("Goals").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_GoalProgress_Goals")
            .FromTable("GoalProgress").ForeignColumn("GoalId")
            .ToTable("Goals").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_Challenges_Users")
            .FromTable("Challenges").ForeignColumn("CreatedBy")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Restrict);

        Create.ForeignKey("FK_UserAchievements_Users")
            .FromTable("UserAchievements").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserAchievements_Achievements")
            .FromTable("UserAchievements").ForeignColumn("AchievementId")
            .ToTable("Achievements").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_ChallengeParticipants_Challenges")
            .FromTable("ChallengeParticipants").ForeignColumn("ChallengeId")
            .ToTable("Challenges").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_ChallengeParticipants_Users")
            .FromTable("ChallengeParticipants").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserFriends_Users_User")
            .FromTable("UserFriends").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserFriends_Users_Friend")
            .FromTable("UserFriends").ForeignColumn("FriendId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Restrict);

        Create.ForeignKey("FK_UserRoles_Users")
            .FromTable("UserRoles").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_UserRoles_Roles")
            .FromTable("UserRoles").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // Indexes
        Create.Index("IX_Users_Email").OnTable("Users").OnColumn("Email").Unique();
        Create.Index("IX_Users_Username").OnTable("Users").OnColumn("Username").Unique();
        Create.Index("IX_Goals_UserId").OnTable("Goals").OnColumn("UserId");
        Create.Index("IX_Goals_DueDate").OnTable("Goals").OnColumn("DueDate");
        Create.Index("IX_Goals_Priority").OnTable("Goals").OnColumn("Priority");
        Create.Index("IX_Goals_IsCompleted").OnTable("Goals").OnColumn("IsCompleted");

        Create.Index("IX_GoalProgress_GoalId").OnTable("GoalProgress").OnColumn("GoalId");
        Create.Index("IX_GoalProgress_CreatedAt").OnTable("GoalProgress").OnColumn("CreatedAt");

        Create.Index("IX_Challenges_CreatedBy").OnTable("Challenges").OnColumn("CreatedBy");
        Create.Index("IX_Challenges_StartDate").OnTable("Challenges").OnColumn("StartDate");
        Create.Index("IX_Challenges_EndDate").OnTable("Challenges").OnColumn("EndDate");

        Create.Index("IX_Achievements_Type").OnTable("Achievements").OnColumn("Type");

        // Составные индексы уже не нужны, так как PK составные
        // Но можно добавить для других комбинаций
        Create.Index("IX_UserFriends_Status").OnTable("UserFriends").OnColumn("Status");
        Create.Index("IX_ChallengeParticipants_Status").OnTable("ChallengeParticipants").OnColumn("Status");
    }

    public override void Down()
    {
        // Drop foreign keys first
        Delete.ForeignKey("FK_UserRoles_Roles").OnTable("UserRoles");
        Delete.ForeignKey("FK_UserRoles_Users").OnTable("UserRoles");
        Delete.ForeignKey("FK_UserFriends_Users_Friend").OnTable("UserFriends");
        Delete.ForeignKey("FK_UserFriends_Users_User").OnTable("UserFriends");
        Delete.ForeignKey("FK_ChallengeParticipants_Users").OnTable("ChallengeParticipants");
        Delete.ForeignKey("FK_ChallengeParticipants_Challenges").OnTable("ChallengeParticipants");
        Delete.ForeignKey("FK_UserAchievements_Achievements").OnTable("UserAchievements");
        Delete.ForeignKey("FK_UserAchievements_Users").OnTable("UserAchievements");
        Delete.ForeignKey("FK_Challenges_Users").OnTable("Challenges");
        Delete.ForeignKey("FK_GoalProgress_Goals").OnTable("GoalProgress");
        Delete.ForeignKey("FK_Goals_Users").OnTable("Goals");

        // Drop tables
        Delete.Table("UserRoles");
        Delete.Table("Roles");
        Delete.Table("UserFriends");
        Delete.Table("ChallengeParticipants");
        Delete.Table("UserAchievements");
        Delete.Table("Challenges");
        Delete.Table("Achievements");
        Delete.Table("GoalProgress");
        Delete.Table("Goals");
        Delete.Table("Users");
    }
}
