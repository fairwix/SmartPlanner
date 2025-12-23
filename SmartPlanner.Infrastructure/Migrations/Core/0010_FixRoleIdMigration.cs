using FluentMigrator;

[Migration(0010)]
public class FixRoleIdMigration : Migration
{
    public override void Up()
    {
        // 1. Сначала обновляем seed пользователей с новыми полями из миграции 0004
        Execute.Sql(@"
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

        // 2. Добавляем колонки из миграции 0005, если их нет
        if (!Schema.Table("Users").Column("PasswordSalt").Exists())
        {
            Alter.Table("Users")
                .AddColumn("PasswordSalt").AsString().NotNullable().WithDefaultValue("")
                .AddColumn("LastLoginAt").AsDateTime().Nullable();

            Execute.Sql(@"
                UPDATE ""Users""
                SET ""PasswordSalt"" = substring(md5(random()::text || clock_timestamp()::text) from 1 for 32)
                WHERE ""PasswordSalt"" = '';
            ");
        }

        // 3. Исправляем таблицу Roles - меняем тип Id на UUID
        Execute.Sql(@"
            -- Создаем временную таблицу для маппинга старых ID на новые GUID
            CREATE TEMP TABLE role_mapping (
                old_id INTEGER,
                new_id UUID
            );

            -- Генерируем новые UUID для существующих ролей
            INSERT INTO role_mapping (old_id, new_id)
            SELECT ""Id"", gen_random_uuid()
            FROM ""Roles"";

            -- Обновляем UserRoles с новыми UUID
            UPDATE ""UserRoles"" ur
            SET ""RoleId"" = CAST(rm.new_id AS TEXT)
            FROM role_mapping rm
            WHERE ur.""RoleId""::INTEGER = rm.old_id;

            -- Обновляем RolePermissions с новыми UUID
            UPDATE ""RolePermissions"" rp
            SET ""RoleId"" = rm.new_id
            FROM role_mapping rm
            WHERE rp.""RoleId""::INTEGER = rm.old_id;

            -- Обновляем Roles.Id на новые UUID
            UPDATE ""Roles"" r
            SET ""Id"" = rm.new_id
            FROM role_mapping rm
            WHERE r.""Id""::INTEGER = rm.old_id;

            -- Удаляем временную таблицу
            DROP TABLE role_mapping;
        ");

        // 4. Теперь меняем тип колонки Id в Roles на UUID
        Execute.Sql(@"
            -- Изменяем тип Id в Roles с INTEGER на UUID
            ALTER TABLE ""Roles""
            ALTER COLUMN ""Id"" TYPE UUID USING ""Id""::UUID;

            -- Изменяем тип RoleId в UserRoles с TEXT на UUID
            ALTER TABLE ""UserRoles""
            ALTER COLUMN ""RoleId"" TYPE UUID USING ""RoleId""::UUID;

            -- Изменяем тип RoleId в RolePermissions с INTEGER на UUID
            ALTER TABLE ""RolePermissions""
            ALTER COLUMN ""RoleId"" TYPE UUID USING ""RoleId""::UUID;
        ");

        // 5. Добавляем недостающие колонки в UserRoles
        if (!Schema.Table("UserRoles").Column("AssignedBy").Exists())
        {
            Alter.Table("UserRoles")
                .AddColumn("AssignedBy").AsGuid().Nullable()
                .AddColumn("AssignedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

            Execute.Sql(@"
                UPDATE ""UserRoles""
                SET ""AssignedBy"" = '00000000-0000-0000-0000-000000000001'
                WHERE ""AssignedBy"" IS NULL;
            ");
        }

        // 6. Добавляем колонку Description в Roles, если ее нет
        if (!Schema.Table("Roles").Column("Description").Exists())
        {
            Alter.Table("Roles")
                .AddColumn("Description").AsString(500).Nullable();

            Execute.Sql(@"
                UPDATE ""Roles""
                SET ""Description"" = CASE ""Name""
                    WHEN 'Admin' THEN 'System administrator with full access'
                    WHEN 'User' THEN 'Regular user with basic permissions'
                    ELSE 'System role'
                END;
            ");
        }

        // 7. Создаем индексы
        Create.Index("IX_UserRoles_RoleId")
            .OnTable("UserRoles")
            .OnColumn("RoleId");

        Create.Index("IX_RolePermissions_RoleId")
            .OnTable("RolePermissions")
            .OnColumn("RoleId");

        Create.Index("IX_Users_LastLoginAt")
            .OnTable("Users")
            .OnColumn("LastLoginAt");

        // 8. Восстанавливаем внешние ключи
        Execute.Sql(@"
            -- Удаляем старые FK
            ALTER TABLE ""UserRoles"" DROP CONSTRAINT IF EXISTS ""FK_UserRoles_Roles"";
            ALTER TABLE ""RolePermissions"" DROP CONSTRAINT IF EXISTS ""FK_RolePermissions_Roles"";

            -- Создаем новые FK с правильными типами
            ALTER TABLE ""UserRoles""
            ADD CONSTRAINT ""FK_UserRoles_Roles""
            FOREIGN KEY (""RoleId"") REFERENCES ""Roles""(""Id"")
            ON DELETE CASCADE;

            ALTER TABLE ""RolePermissions""
            ADD CONSTRAINT ""FK_RolePermissions_Roles""
            FOREIGN KEY (""RoleId"") REFERENCES ""Roles""(""Id"")
            ON DELETE CASCADE;
        ");
    }

    public override void Down()
    {
        // Удаляем индексы
        Delete.Index("IX_Users_LastLoginAt").OnTable("Users");
        Delete.Index("IX_RolePermissions_RoleId").OnTable("RolePermissions");
        Delete.Index("IX_UserRoles_RoleId").OnTable("UserRoles");

        // Удаляем FK
        Execute.Sql(@"
            ALTER TABLE ""RolePermissions"" DROP CONSTRAINT IF EXISTS ""FK_RolePermissions_Roles"";
            ALTER TABLE ""UserRoles"" DROP CONSTRAINT IF EXISTS ""FK_UserRoles_Roles"";
        ");

        // Меняем типы обратно на INTEGER (упрощенная версия)
        Execute.Sql(@"
            -- Восстанавливаем старые значения для упрощения отката
            UPDATE ""UserRoles"" SET ""RoleId"" = '1' WHERE ""RoleId"" = (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'Admin');
            UPDATE ""UserRoles"" SET ""RoleId"" = '2' WHERE ""RoleId"" = (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'User');
            UPDATE ""RolePermissions"" SET ""RoleId"" = 1 WHERE ""RoleId"" = (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'Admin');
            UPDATE ""RolePermissions"" SET ""RoleId"" = 2 WHERE ""RoleId"" = (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'User');
        ");

        // Удаляем добавленные колонки
        if (Schema.Table("Roles").Column("Description").Exists())
        {
            Delete.Column("Description").FromTable("Roles");
        }

        if (Schema.Table("UserRoles").Column("AssignedBy").Exists())
        {
            Delete.Column("AssignedBy").FromTable("UserRoles");
            Delete.Column("AssignedAt").FromTable("UserRoles");
        }

        if (Schema.Table("Users").Column("PasswordSalt").Exists())
        {
            Delete.Column("PasswordSalt").FromTable("Users");
            Delete.Column("LastLoginAt").FromTable("Users");
        }
    }
}
