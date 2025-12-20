    // Infrastructure/Migrations/0007_CompleteAuth.cs
    using FluentMigrator;

    [Migration(0007)]
    public class CompleteAuth : Migration
    {
        public override void Up()
        {
            // 1. Добавляем недостающие поля в Users (если их нет)
            if (!Schema.Table("Users").Column("FirstName").Exists())
            {
                Alter.Table("Users")
                    .AddColumn("FirstName").AsString(100).Nullable()
                    .AddColumn("LastName").AsString(100).Nullable()
                    .AddColumn("DateOfBirth").AsDateTime().Nullable()
                    .AddColumn("PhoneNumber").AsString(20).Nullable()
                    .AddColumn("EmailConfirmedAt").AsDateTime().Nullable()
                    .AddColumn("IsEmailConfirmed").AsBoolean().NotNullable().WithDefaultValue(false)
                    .AddColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .AddColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);
            }

            // 2. Обновляем существующих пользователей
            Execute.Sql(@"
                -- Обновляем seed пользователей с новыми полями
                UPDATE ""Users"" SET
                    ""FirstName"" = CASE
                        WHEN ""Username"" = 'admin' THEN 'System'
                        WHEN ""Username"" = 'testuser' THEN 'Test'
                        WHEN ""Username"" = 'john_doe' THEN 'John'
                        ELSE 'User'
                    END,
                    ""LastName"" = CASE
                        WHEN ""Username"" = 'admin' THEN 'Administrator'
                        WHEN ""Username"" = 'testuser' THEN 'User'
                        WHEN ""Username"" = 'john_doe' THEN 'Doe'
                        ELSE ''
                    END,
                    ""IsEmailConfirmed"" = true,
                    ""IsActive"" = true,
                    ""IsDeleted"" = false
                WHERE ""FirstName"" IS NULL;
            ");

            // 3. Добавляем недостающие permissions (до 7 как в ТЗ)
            Execute.Sql(@"
                -- Дополняем до 7 permissions для User role
                INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""Category"", ""CreatedAt"") VALUES
                ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'Achievement.View', 'View achievements', 'Achievements', NOW()),
                ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'Achievement.Award', 'Award achievements', 'Achievements', NOW()),
                ('ffffffff-ffff-ffff-ffff-ffffffffffff', 'Friend.Manage', 'Manage friends', 'Social', NOW()),
                ('gggggggg-gggg-gggg-gggg-gggggggggggg', 'Profile.Edit', 'Edit own profile', 'Profile', NOW()),
                ('hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh', 'Challenge.Join', 'Join challenges', 'Challenges', NOW())
                ON CONFLICT (""Name"") DO NOTHING;

                -- User role получает 7 permissions (как в ТЗ)
                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""AssignedAt"")
                SELECT r.""Id"", p.""Id"", NOW()
                FROM ""Roles"" r, ""Permissions"" p
                WHERE r.""Name"" = 'User'
                AND p.""Name"" IN (
                    'User.View', 'Goal.View', 'Goal.Create', 'Goal.Edit',
                    'Challenge.View', 'Challenge.Create', 'Achievement.View'
                )
                ON CONFLICT (""RoleId"", ""PermissionId"") DO NOTHING;
            ");
        }

        public override void Down()
        {
            // Безопасный откат - не удаляем таблицы, только добавленные поля
            if (Schema.Table("Users").Column("FirstName").Exists())
            {
                Delete.Column("FirstName").FromTable("Users");
                Delete.Column("LastName").FromTable("Users");
                Delete.Column("DateOfBirth").FromTable("Users");
                Delete.Column("PhoneNumber").FromTable("Users");
                Delete.Column("EmailConfirmedAt").FromTable("Users");
                Delete.Column("IsEmailConfirmed").FromTable("Users");
                Delete.Column("IsActive").FromTable("Users");
                Delete.Column("IsDeleted").FromTable("Users");
            }

            // Удаляем добавленные permissions
            Execute.Sql(@"
                DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (
                    SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" IN (
                        'Achievement.View', 'Achievement.Award', 'Friend.Manage',
                        'Profile.Edit', 'Challenge.Join'
                    )
                );

                DELETE FROM ""Permissions"" WHERE ""Name"" IN (
                    'Achievement.View', 'Achievement.Award', 'Friend.Manage',
                    'Profile.Edit', 'Challenge.Join'
                );
            ");
        }
    }
