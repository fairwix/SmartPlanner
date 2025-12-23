using System;
using FluentMigrator;

[Migration(0005)]
public class CorrectAuthModels : Migration
{
    public override void Up()
    {

        Execute.Sql(@"
            -- Временная таблица для маппинга старых ID на новые GUID
            CREATE TEMP TABLE role_id_mapping (
                old_id INT,
                new_id UUID
            );

            -- Генерируем новые GUID для существующих ролей
            INSERT INTO role_id_mapping (old_id, new_id)
            SELECT ""Id"", gen_random_uuid()
            FROM ""Roles"";

            -- Обновляем UserRoles.RoleId на новые GUID
            UPDATE ""UserRoles"" ur
            SET ""RoleId"" = CAST(rim.new_id AS TEXT)
            FROM role_id_mapping rim
            WHERE ur.""RoleId""::INT = rim.old_id;

            -- Обновляем RolePermissions.RoleId на новые GUID
            UPDATE ""RolePermissions"" rp
            SET ""RoleId"" = CAST(rim.new_id AS TEXT)::INTEGER
            FROM role_id_mapping rim
            WHERE rp.""RoleId"" = rim.old_id;

            -- Обновляем Roles.Id на новые GUID
            UPDATE ""Roles"" r
            SET ""Id"" = rim.new_id
            FROM role_id_mapping rim
            WHERE r.""Id"" = rim.old_id;

            -- Удаляем временную таблицу
            DROP TABLE role_id_mapping;
        ");

        Execute.Sql(@"
            -- Создаем временную таблицу с новой структурой
            CREATE TABLE ""Roles_New"" (
                ""Id"" UUID PRIMARY KEY NOT NULL,
                ""Name"" VARCHAR(50) NOT NULL,
                ""NormalizedName"" VARCHAR(50) NOT NULL,
                ""Description"" VARCHAR(500) NULL,
                ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
            );

            -- Копируем данные
            INSERT INTO ""Roles_New"" (""Id"", ""Name"", ""NormalizedName"", ""Description"", ""CreatedAt"")
            SELECT ""Id"", ""Name"", ""NormalizedName"", '', ""CreatedAt""
            FROM ""Roles"";

            -- Переименовываем таблицы
            ALTER TABLE ""Roles"" RENAME TO ""Roles_Old"";
            ALTER TABLE ""Roles_New"" RENAME TO ""Roles"";

            -- Удаляем старую таблицу
            DROP TABLE ""Roles_Old"";
        ");

        Alter.Table("Users")
            .AddColumn("PasswordSalt").AsString().NotNullable().WithDefaultValue("")
            .AddColumn("LastLoginAt").AsDateTime().Nullable();

        Execute.Sql(@"
            UPDATE ""Users""
            SET ""PasswordSalt"" = substring(md5(random()::text || clock_timestamp()::text) from 1 for 32)
            WHERE ""PasswordSalt"" = '';
        ");

        Execute.Sql(@"
            -- Создаем новую таблицу UserRoles
            CREATE TABLE ""UserRoles_New"" (
                ""UserId"" UUID NOT NULL,
                ""RoleId"" UUID NOT NULL,
                ""AssignedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""AssignedBy"" UUID NULL,
                PRIMARY KEY (""UserId"", ""RoleId"")
            );

            -- Копируем данные (преобразуем RoleId из текста в UUID)
            INSERT INTO ""UserRoles_New"" (""UserId"", ""RoleId"", ""AssignedAt"", ""AssignedBy"")
            SELECT
                ur.""UserId"",
                CAST(ur.""RoleId"" AS UUID),
                ur.""CreatedAt"",
                NULL
            FROM ""UserRoles"" ur;

            -- Переименовываем таблицы
            ALTER TABLE ""UserRoles"" RENAME TO ""UserRoles_Old"";
            ALTER TABLE ""UserRoles_New"" RENAME TO ""UserRoles"";

            -- Удаляем старую таблицу
            DROP TABLE ""UserRoles_Old"";

            -- Восстанавливаем foreign keys
            ALTER TABLE ""UserRoles""
            ADD CONSTRAINT ""FK_UserRoles_Users""
            FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"")
            ON DELETE CASCADE;

            ALTER TABLE ""UserRoles""
            ADD CONSTRAINT ""FK_UserRoles_Roles""
            FOREIGN KEY (""RoleId"") REFERENCES ""Roles""(""Id"")
            ON DELETE CASCADE;
        ");

        Execute.Sql(@"
            -- Изменяем тип RoleId в RolePermissions
            ALTER TABLE ""RolePermissions""
            ALTER COLUMN ""RoleId"" TYPE UUID USING CAST(""RoleId"" AS TEXT)::UUID;

            -- Восстанавливаем foreign key
            ALTER TABLE ""RolePermissions""
            ADD CONSTRAINT ""FK_RolePermissions_Roles""
            FOREIGN KEY (""RoleId"") REFERENCES ""Roles""(""Id"")
            ON DELETE CASCADE;
        ");

        Execute.Sql(@"
            UPDATE ""Roles""
            SET ""Description"" = CASE ""Name""
                WHEN 'Admin' THEN 'System administrator with full access'
                WHEN 'User' THEN 'Regular user with basic permissions'
                ELSE 'System role'
            END;
        ");

        Execute.Sql(@"
            UPDATE ""UserRoles""
            SET ""AssignedBy"" = '00000000-0000-0000-0000-000000000001' -- admin user ID
            WHERE ""AssignedBy"" IS NULL;
        ");

        Create.Index("IX_UserRoles_RoleId")
            .OnTable("UserRoles")
            .OnColumn("RoleId");

        Create.Index("IX_RolePermissions_RoleId")
            .OnTable("RolePermissions")
            .OnColumn("RoleId");

        Create.Index("IX_Users_LastLoginAt")
            .OnTable("Users")
            .OnColumn("LastLoginAt");
    }

    public override void Down()
    {

        Delete.Index("IX_Users_LastLoginAt").OnTable("Users");
        Delete.Index("IX_RolePermissions_RoleId").OnTable("RolePermissions");
        Delete.Index("IX_UserRoles_RoleId").OnTable("UserRoles");

        Execute.Sql(@"
            ALTER TABLE ""RolePermissions""
            ALTER COLUMN ""RoleId"" TYPE INTEGER USING 1;
        ");


        Delete.Column("PasswordSalt").FromTable("Users");
        Delete.Column("LastLoginAt").FromTable("Users");
        Delete.Column("AssignedBy").FromTable("UserRoles");
        Delete.Column("Description").FromTable("Roles");

    }
}
