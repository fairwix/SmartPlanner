using FluentMigrator;
using System;
using System.Data;

[Migration(0004)]
public class AddAuthTables : Migration
{
    public override void Up()
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

        Create.Table("Permissions")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Description").AsString(500).Nullable()
            .WithColumn("Category").AsString(100).Nullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

        Create.Index("IX_Permissions_Name")
            .OnTable("Permissions")
            .OnColumn("Name")
            .Unique();

        Create.Table("UserClaims")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("ClaimType").AsString(100).NotNullable()
            .WithColumn("ClaimValue").AsString(500).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_UserClaims_Users")
            .FromTable("UserClaims").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDeleteOrUpdate(Rule.Cascade);

        Create.Index("IX_UserClaims_UserId_ClaimType")
            .OnTable("UserClaims")
            .OnColumn("UserId")
            .Ascending()
            .OnColumn("ClaimType")
            .Ascending();

        Create.Table("UserSessions")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("RefreshTokenHash").AsString(256).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("DeviceInfo").AsString(500).Nullable()
            .WithColumn("IpAddress").AsString(45).Nullable()
            .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("RevokedAt").AsDateTime().Nullable();

        Create.ForeignKey("FK_UserSessions_Users")
            .FromTable("UserSessions").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDeleteOrUpdate(Rule.Cascade);

        Create.Index("IX_UserSessions_UserId_ExpiresAt")
            .OnTable("UserSessions")
            .OnColumn("UserId")
            .Ascending()
            .OnColumn("ExpiresAt")
            .Descending();

        Create.Index("IX_UserSessions_RefreshTokenHash")
            .OnTable("UserSessions")
            .OnColumn("RefreshTokenHash");

        Create.Table("RolePermissions")
            .WithColumn("RoleId").AsInt32().NotNullable()
            .WithColumn("PermissionId").AsGuid().NotNullable()
            .WithColumn("AssignedAt").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
            .WithColumn("AssignedBy").AsGuid().Nullable();

        Create.PrimaryKey("PK_RolePermissions")
            .OnTable("RolePermissions")
            .Columns("RoleId", "PermissionId");

        Create.ForeignKey("FK_RolePermissions_Roles")
            .FromTable("RolePermissions").ForeignColumn("RoleId")
            .ToTable("Roles").PrimaryColumn("Id")
            .OnDeleteOrUpdate(Rule.Cascade);

        Create.ForeignKey("FK_RolePermissions_Permissions")
            .FromTable("RolePermissions").ForeignColumn("PermissionId")
            .ToTable("Permissions").PrimaryColumn("Id")
            .OnDeleteOrUpdate(Rule.Cascade);

        Execute.Sql(@"
            -- EmailConfirmedAt должен быть после или в момент создания
            ALTER TABLE ""Users""
            ADD CONSTRAINT ""CK_Users_EmailConfirmedAt_Valid""
            CHECK (""EmailConfirmedAt"" IS NULL OR ""EmailConfirmedAt"" >= ""CreatedAt"");

            -- IsActive и IsDeleted логически связаны
            ALTER TABLE ""Users""
            ADD CONSTRAINT ""CK_Users_Status_Valid""
            CHECK (NOT (""IsActive"" = false AND ""IsDeleted"" = true));

            -- ExpiresAt должен быть после CreatedAt для сессий
            ALTER TABLE ""UserSessions""
            ADD CONSTRAINT ""CK_UserSessions_ExpiresAt_Valid""
            CHECK (""ExpiresAt"" > ""CreatedAt"");
        ");

        Execute.Sql(@"
            -- Добавляем основные permissions (минимум 5-7 согласно ТЗ)
            INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""Category"", ""CreatedAt"") VALUES
            -- Users permissions
            ('11111111-1111-1111-1111-111111111111', 'User.View', 'View user profiles', 'Users', NOW()),
            ('22222222-2222-2222-2222-222222222222', 'User.Edit', 'Edit user profiles', 'Users', NOW()),
            ('33333333-3333-3333-3333-333333333333', 'User.Delete', 'Delete users', 'Users', NOW()),

            -- Goals permissions
            ('44444444-4444-4444-4444-444444444444', 'Goal.View', 'View goals', 'Goals', NOW()),
            ('55555555-5555-5555-5555-555555555555', 'Goal.Create', 'Create goals', 'Goals', NOW()),
            ('66666666-6666-6666-6666-666666666666', 'Goal.Edit', 'Edit goals', 'Goals', NOW()),
            ('77777777-7777-7777-7777-777777777777', 'Goal.Delete', 'Delete goals', 'Goals', NOW()),

            -- Challenges permissions
            ('88888888-8888-8888-8888-888888888888', 'Challenge.View', 'View challenges', 'Challenges', NOW()),
            ('99999999-9999-9999-9999-999999999999', 'Challenge.Create', 'Create challenges', 'Challenges', NOW()),
            ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Challenge.Edit', 'Edit challenges', 'Challenges', NOW()),

            -- Admin permissions (дополнительные)
            ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Admin.ViewStats', 'View system statistics', 'Admin', NOW()),
            ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'Admin.ManageUsers', 'Manage all users', 'Admin', NOW())
            ON CONFLICT (""Name"") DO NOTHING;
        ");

        Execute.Sql(@"
            -- Admin role получает все permissions (ТЗ требует роль Admin)
            INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""AssignedAt"")
            SELECT 1, p.""Id"", NOW()
            FROM ""Permissions"" p
            ON CONFLICT (""RoleId"", ""PermissionId"") DO NOTHING;

            -- User role получает базовые permissions
            INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""AssignedAt"") VALUES
            (2, '11111111-1111-1111-1111-111111111111', NOW()),  -- User.View
            (2, '44444444-4444-4444-4444-444444444444', NOW()),  -- Goal.View
            (2, '55555555-5555-5555-5555-555555555555', NOW()),  -- Goal.Create
            (2, '66666666-6666-6666-6666-666666666666', NOW()),  -- Goal.Edit
            (2, '88888888-8888-8888-8888-888888888888', NOW()),  -- Challenge.View
            (2, '99999999-9999-9999-9999-999999999999', NOW())   -- Challenge.Create
            ON CONFLICT (""RoleId"", ""PermissionId"") DO NOTHING;
        ");
    }

    public override void Down()
    {

        Execute.Sql("DELETE FROM \"RolePermissions\";");

        Execute.Sql(@"
            ALTER TABLE ""Users"" DROP CONSTRAINT IF EXISTS ""CK_Users_EmailConfirmedAt_Valid"";
            ALTER TABLE ""Users"" DROP CONSTRAINT IF EXISTS ""CK_Users_Status_Valid"";
            ALTER TABLE ""UserSessions"" DROP CONSTRAINT IF EXISTS ""CK_UserSessions_ExpiresAt_Valid"";
        ");

        Delete.Index("IX_UserSessions_UserId_ExpiresAt").OnTable("UserSessions");
        Delete.Index("IX_UserSessions_RefreshTokenHash").OnTable("UserSessions");
        Delete.Index("IX_UserClaims_UserId_ClaimType").OnTable("UserClaims");
        Delete.Index("IX_Permissions_Name").OnTable("Permissions");

        Delete.ForeignKey("FK_RolePermissions_Permissions").OnTable("RolePermissions");
        Delete.ForeignKey("FK_RolePermissions_Roles").OnTable("RolePermissions");
        Delete.ForeignKey("FK_UserClaims_Users").OnTable("UserClaims");
        Delete.ForeignKey("FK_UserSessions_Users").OnTable("UserSessions");

        Delete.Table("RolePermissions");
        Delete.Table("UserClaims");
        Delete.Table("UserSessions");
        Delete.Table("Permissions");

        Delete.Column("FirstName").FromTable("Users");
        Delete.Column("LastName").FromTable("Users");
        Delete.Column("DateOfBirth").FromTable("Users");
        Delete.Column("PhoneNumber").FromTable("Users");
        Delete.Column("EmailConfirmedAt").FromTable("Users");
        Delete.Column("IsEmailConfirmed").FromTable("Users");
        Delete.Column("IsActive").FromTable("Users");
        Delete.Column("IsDeleted").FromTable("Users");
    }
}
