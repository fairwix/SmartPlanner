// SmartPlanner.Infrastructure/Migrations/0008_AddPasswordResetTokens.cs
using FluentMigrator;

[Migration(0008)]
public class AddPasswordResetTokens : Migration
{
    public override void Up()
    {
        // Таблица PasswordResetTokens
        Create.Table("PasswordResetTokens")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TokenHash").AsString(256).NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("IsUsed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("UsedAt").AsDateTime().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // Таблица EmailConfirmationTokens
        Create.Table("EmailConfirmationTokens")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TokenHash").AsString(256).NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("IsUsed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("UsedAt").AsDateTime().Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        // Foreign Keys
        Create.ForeignKey("FK_PasswordResetTokens_Users")
            .FromTable("PasswordResetTokens").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("FK_EmailConfirmationTokens_Users")
            .FromTable("EmailConfirmationTokens").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // Индексы
        Create.Index("IX_PasswordResetTokens_TokenHash")
            .OnTable("PasswordResetTokens")
            .OnColumn("TokenHash")
            .Unique();

        Create.Index("IX_EmailConfirmationTokens_TokenHash")
            .OnTable("EmailConfirmationTokens")
            .OnColumn("TokenHash")
            .Unique();

        Create.Index("IX_PasswordResetTokens_ExpiresAt")
            .OnTable("PasswordResetTokens")
            .OnColumn("ExpiresAt");

        Create.Index("IX_EmailConfirmationTokens_ExpiresAt")
            .OnTable("EmailConfirmationTokens")
            .OnColumn("ExpiresAt");

        // Индексы для запросов по пользователю
        Create.Index("IX_PasswordResetTokens_UserId_IsUsed")
            .OnTable("PasswordResetTokens")
            .OnColumn("UserId")
            .Ascending()
            .OnColumn("IsUsed")
            .Ascending();

        Create.Index("IX_EmailConfirmationTokens_UserId_IsUsed")
            .OnTable("EmailConfirmationTokens")
            .OnColumn("UserId")
            .Ascending()
            .OnColumn("IsUsed")
            .Ascending();

        // ✅ ДОБАВЛЯЕМ CONSTRAINTS ДЛЯ БИЗНЕС-ЛОГИКИ
        Execute.Sql(@"
            -- PasswordResetTokens constraints
            ALTER TABLE ""PasswordResetTokens""
            ADD CONSTRAINT ""CK_PasswordResetTokens_ExpiresAt_Future""
            CHECK (""ExpiresAt"" > ""CreatedAt"");

            ALTER TABLE ""PasswordResetTokens""
            ADD CONSTRAINT ""CK_PasswordResetTokens_UsedAt_Valid""
            CHECK (""UsedAt"" IS NULL OR ""UsedAt"" >= ""CreatedAt"");

            -- EmailConfirmationTokens constraints
            ALTER TABLE ""EmailConfirmationTokens""
            ADD CONSTRAINT ""CK_EmailConfirmationTokens_ExpiresAt_Future""
            CHECK (""ExpiresAt"" > ""CreatedAt"");

            ALTER TABLE ""EmailConfirmationTokens""
            ADD CONSTRAINT ""CK_EmailConfirmationTokens_UsedAt_Valid""
            CHECK (""UsedAt"" IS NULL OR ""UsedAt"" >= ""CreatedAt"");

            -- Если UsedAt заполнено, то IsUsed должен быть true
            ALTER TABLE ""PasswordResetTokens""
            ADD CONSTRAINT ""CK_PasswordResetTokens_UsedConsistency""
            CHECK ((""UsedAt"" IS NULL AND ""IsUsed"" = false) OR
                   (""UsedAt"" IS NOT NULL AND ""IsUsed"" = true));

            ALTER TABLE ""EmailConfirmationTokens""
            ADD CONSTRAINT ""CK_EmailConfirmationTokens_UsedConsistency""
            CHECK ((""UsedAt"" IS NULL AND ""IsUsed"" = false) OR
                   (""UsedAt"" IS NOT NULL AND ""IsUsed"" = true));
        ");
    }

    public override void Down()
    {
        // Удаляем constraints
        Execute.Sql(@"
            ALTER TABLE ""PasswordResetTokens""
            DROP CONSTRAINT IF EXISTS ""CK_PasswordResetTokens_ExpiresAt_Future"";

            ALTER TABLE ""PasswordResetTokens""
            DROP CONSTRAINT IF EXISTS ""CK_PasswordResetTokens_UsedAt_Valid"";

            ALTER TABLE ""PasswordResetTokens""
            DROP CONSTRAINT IF EXISTS ""CK_PasswordResetTokens_UsedConsistency"";

            ALTER TABLE ""EmailConfirmationTokens""
            DROP CONSTRAINT IF EXISTS ""CK_EmailConfirmationTokens_ExpiresAt_Future"";

            ALTER TABLE ""EmailConfirmationTokens""
            DROP CONSTRAINT IF EXISTS ""CK_EmailConfirmationTokens_UsedAt_Valid"";

            ALTER TABLE ""EmailConfirmationTokens""
            DROP CONSTRAINT IF EXISTS ""CK_EmailConfirmationTokens_UsedConsistency"";
        ");

        // Удаляем индексы
        Delete.Index("IX_EmailConfirmationTokens_UserId_IsUsed").OnTable("EmailConfirmationTokens");
        Delete.Index("IX_PasswordResetTokens_UserId_IsUsed").OnTable("PasswordResetTokens");
        Delete.Index("IX_EmailConfirmationTokens_ExpiresAt").OnTable("EmailConfirmationTokens");
        Delete.Index("IX_PasswordResetTokens_ExpiresAt").OnTable("PasswordResetTokens");
        Delete.Index("IX_EmailConfirmationTokens_TokenHash").OnTable("EmailConfirmationTokens");
        Delete.Index("IX_PasswordResetTokens_TokenHash").OnTable("PasswordResetTokens");

        // Удаляем foreign keys
        Delete.ForeignKey("FK_EmailConfirmationTokens_Users").OnTable("EmailConfirmationTokens");
        Delete.ForeignKey("FK_PasswordResetTokens_Users").OnTable("PasswordResetTokens");

        // Удаляем таблицы
        Delete.Table("EmailConfirmationTokens");
        Delete.Table("PasswordResetTokens");
    }
}
