using System;
using System.Data;
using FluentMigrator;

[Migration(0001)]
public class InitialMigration : Migration
{
    public override void Up()
    {
        // ========== Users table ==========
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Username").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("PasswordHash").AsString().NotNullable()
            .WithColumn("Balance").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("StreakCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LastLogin").AsDateTime().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== Goals table ==========
        Create.Table("Goals")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Title").AsString(500).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Category").AsInt32().NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable()
            .WithColumn("DueDate").AsDateTime().NotNullable()
            .WithColumn("TargetValue").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("CurrentValue").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("IsCompleted").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("IsAiGenerated").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("RewardAmount").AsInt32().NotNullable().WithDefaultValue(10)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== GoalProgress table ==========
        Create.Table("GoalProgress")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("GoalId").AsGuid().NotNullable()
            .WithColumn("Value").AsInt32().NotNullable()
            .WithColumn("PreviousValue").AsInt32().NotNullable()
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== Achievements table ==========
        Create.Table("Achievements")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("BadgeImage").AsString(500).Nullable()
            .WithColumn("RewardAmount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("Type").AsInt32().NotNullable()
            .WithColumn("Condition").AsString(500).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== Challenges table ==========
        Create.Table("Challenges")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(2000).Nullable()
            .WithColumn("Type").AsInt32().NotNullable()
            .WithColumn("StartDate").AsDateTime().NotNullable()
            .WithColumn("EndDate").AsDateTime().NotNullable()
            .WithColumn("IsGroupChallenge").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("TargetValue").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CurrentValue").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CreatedBy").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== UserAchievements junction table ==========
        Create.Table("UserAchievements")
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("AchievementId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("AwardedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable();

        // ========== ChallengeParticipants junction table ==========
        Create.Table("ChallengeParticipants")
            .WithColumn("ChallengeId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Status").AsInt32().NotNullable().WithDefaultValue(1) // 1 = Joined
            .WithColumn("JoinedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("PersonalContribution").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().Nullable();

        // ========== UserFriends junction table ==========
        Create.Table("UserFriends")
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("FriendId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== Roles table ==========
        Create.Table("Roles")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("NormalizedName").AsString(50).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== UserRoles junction table ==========
        Create.Table("UserRoles")
            .WithColumn("UserId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("RoleId").AsInt32().NotNullable().PrimaryKey()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // ========== Foreign Keys ==========
        // Goals -> Users
        Create.ForeignKey("FK_Goals_Users")
            .FromTable("Goals").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // GoalProgress -> Goals
        Create.ForeignKey("FK_GoalProgress_Goals")
            .FromTable("GoalProgress").ForeignColumn("GoalId")
            .ToTable("Goals").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // Challenges -> Users (Creator)
        Create.ForeignKey("FK_Challenges_Users")
            .FromTable("Challenges").ForeignColumn("CreatedBy")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // UserAchievements -> Users
        Create.ForeignKey("FK_UserAchievements_Users")
            .FromTable("UserAchievements").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // UserAchievements -> Achievements
        Create.ForeignKey("FK_UserAchievements_Achievements")
            .FromTable("UserAchievements").ForeignColumn("AchievementId")
            .ToTable("Achievements").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // ChallengeParticipants -> Challenges
        Create.ForeignKey("FK_ChallengeParticipants_Challenges")
            .FromTable("ChallengeParticipants").ForeignColumn("ChallengeId")
            .ToTable("Challenges").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // ChallengeParticipants -> Users
        Create.ForeignKey("FK_ChallengeParticipants_Users")
            .FromTable("ChallengeParticipants").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // UserFriends -> Users (User)
        Create.ForeignKey("FK_UserFriends_Users_User")
            .FromTable("UserFriends").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // UserFriends -> Users (Friend)
        Create.ForeignKey("FK_UserFriends_Users_Friend")
            .FromTable("UserFriends").ForeignColumn("FriendId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        // UserRoles -> Users
        Create.ForeignKey("FK_UserRoles_Users")
            .FromTable("UserRoles").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // UserRoles -> Roles
        Create.ForeignKey("FK_UserRoles_Roles")
            .FromTable("UserRoles").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // ========== Indexes ==========
        // Users
        Create.Index("IX_Users_Email").OnTable("Users").OnColumn("Email").Unique();
        Create.Index("IX_Users_Username").OnTable("Users").OnColumn("Username").Unique();
        Create.Index("IX_Users_LastLogin").OnTable("Users").OnColumn("LastLogin");

        // Goals
        Create.Index("IX_Goals_UserId").OnTable("Goals").OnColumn("UserId");
        Create.Index("IX_Goals_DueDate").OnTable("Goals").OnColumn("DueDate");
        Create.Index("IX_Goals_Priority").OnTable("Goals").OnColumn("Priority");
        Create.Index("IX_Goals_IsCompleted").OnTable("Goals").OnColumn("IsCompleted");
        Create.Index("IX_Goals_Category").OnTable("Goals").OnColumn("Category");

        // GoalProgress
        Create.Index("IX_GoalProgress_GoalId").OnTable("GoalProgress").OnColumn("GoalId");
        Create.Index("IX_GoalProgress_CreatedAt").OnTable("GoalProgress").OnColumn("CreatedAt");

        // Challenges
        Create.Index("IX_Challenges_CreatedBy").OnTable("Challenges").OnColumn("CreatedBy");
        Create.Index("IX_Challenges_StartDate").OnTable("Challenges").OnColumn("StartDate");
        Create.Index("IX_Challenges_EndDate").OnTable("Challenges").OnColumn("EndDate");
        Create.Index("IX_Challenges_Type").OnTable("Challenges").OnColumn("Type");

        // Achievements
        Create.Index("IX_Achievements_Type").OnTable("Achievements").OnColumn("Type");

        // UserFriends
        Create.Index("IX_UserFriends_Status").OnTable("UserFriends").OnColumn("Status");

        // ChallengeParticipants
        Create.Index("IX_ChallengeParticipants_Status").OnTable("ChallengeParticipants").OnColumn("Status");

        // ========== Constraints ==========
        Execute.Sql(@"
            -- Goals constraints
            ALTER TABLE ""Goals""
            ADD CONSTRAINT ""CK_Goals_Title_Required""
            CHECK (""Title"" IS NOT NULL AND LENGTH(TRIM(""Title"")) > 0);

            ALTER TABLE ""Goals""
            ADD CONSTRAINT ""CK_Goals_TargetValue_Positive""
            CHECK (""TargetValue"" > 0);

            ALTER TABLE ""Goals""
            ADD CONSTRAINT ""CK_Goals_CurrentValue_Range""
            CHECK (""CurrentValue"" >= 0 AND ""CurrentValue"" <= ""TargetValue"");

            ALTER TABLE ""Goals""
            ADD CONSTRAINT ""CK_Goals_DueDate_Future""
            CHECK (""DueDate"" > ""CreatedAt"");

            -- Challenges constraints
            ALTER TABLE ""Challenges""
            ADD CONSTRAINT ""CK_Challenges_Dates_Valid""
            CHECK (""EndDate"" > ""StartDate"");

            ALTER TABLE ""Challenges""
            ADD CONSTRAINT ""CK_Challenges_TargetValue_Positive""
            CHECK (""TargetValue"" >= 0);

            ALTER TABLE ""Challenges""
            ADD CONSTRAINT ""CK_Challenges_CurrentValue_Range""
            CHECK (""CurrentValue"" >= 0 AND ""CurrentValue"" <= ""TargetValue"");

            -- Users constraints
            ALTER TABLE ""Users""
            ADD CONSTRAINT ""CK_Users_Balance_NonNegative""
            CHECK (""Balance"" >= 0);

            ALTER TABLE ""Users""
            ADD CONSTRAINT ""CK_Users_StreakCount_NonNegative""
            CHECK (""StreakCount"" >= 0);

            ALTER TABLE ""Users""
            ADD CONSTRAINT ""CK_Users_Email_Valid""
            CHECK (""Email"" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');
        ");
    }

    public override void Down()
    {
        // Удаляем constraints
        Execute.Sql(@"
            ALTER TABLE ""Goals"" DROP CONSTRAINT IF EXISTS ""CK_Goals_Title_Required"";
            ALTER TABLE ""Goals"" DROP CONSTRAINT IF EXISTS ""CK_Goals_TargetValue_Positive"";
            ALTER TABLE ""Goals"" DROP CONSTRAINT IF EXISTS ""CK_Goals_CurrentValue_Range"";
            ALTER TABLE ""Goals"" DROP CONSTRAINT IF EXISTS ""CK_Goals_DueDate_Future"";

            ALTER TABLE ""Challenges"" DROP CONSTRAINT IF EXISTS ""CK_Challenges_Dates_Valid"";
            ALTER TABLE ""Challenges"" DROP CONSTRAINT IF EXISTS ""CK_Challenges_TargetValue_Positive"";
            ALTER TABLE ""Challenges"" DROP CONSTRAINT IF EXISTS ""CK_Challenges_CurrentValue_Range"";

            ALTER TABLE ""Users"" DROP CONSTRAINT IF EXISTS ""CK_Users_Balance_NonNegative"";
            ALTER TABLE ""Users"" DROP CONSTRAINT IF EXISTS ""CK_Users_StreakCount_NonNegative"";
            ALTER TABLE ""Users"" DROP CONSTRAINT IF EXISTS ""CK_Users_Email_Valid"";
        ");

        // Удаляем foreign keys
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

        // Удаляем таблицы в обратном порядке
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
