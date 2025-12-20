using System;
using FluentMigrator;

[Migration(0006)]
public class UpdateSeedData : Migration
{
    public override void Up()
    {
        // 1. Обновляем данные пользователей с PasswordSalt
        Execute.Sql(@"
            -- Обновляем администратора
            UPDATE ""Users""
            SET
                ""PasswordSalt"" = 'adminsalt123456789012345678901',
                ""LastLoginAt"" = NOW(),
                ""FirstName"" = 'System',
                ""LastName"" = 'Administrator'
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000001';

            -- Обновляем testuser
            UPDATE ""Users""
            SET
                ""PasswordSalt"" = 'testsalt1234567890123456789012',
                ""LastLoginAt"" = NOW() - INTERVAL '1 day',
                ""FirstName"" = 'Test',
                ""LastName"" = 'User'
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000002';

            -- Обновляем john_doe
            UPDATE ""Users""
            SET
                ""PasswordSalt"" = 'johnsalt123456789012345678901',
                ""LastLoginAt"" = NOW() - INTERVAL '3 days',
                ""FirstName"" = 'John',
                ""LastName"" = 'Doe'
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000003';
        ");

        // 2. Добавляем дополнительные роли (если нужно)
        Execute.Sql(@"
            INSERT INTO ""Roles"" (""Id"", ""Name"", ""NormalizedName"", ""Description"", ""CreatedAt"") VALUES
            ('44444444-4444-4444-4444-444444444444', 'Moderator', 'MODERATOR', 'Content moderator with limited admin rights', NOW()),
            ('55555555-5555-5555-5555-555555555555', 'Premium', 'PREMIUM', 'Premium user with extended features', NOW())
            ON CONFLICT (""Name"") DO NOTHING;
        ");

        // 3. Обновляем описания существующих ролей
        Execute.Sql(@"
            UPDATE ""Roles""
            SET ""Description"" =
                CASE ""Name""
                    WHEN 'Admin' THEN 'Full system administrator with all permissions'
                    WHEN 'User' THEN 'Standard user with basic application access'
                    ELSE ""Description""
                END;
        ");

        // 4. Назначаем дополнительные permissions для новых ролей
        Execute.Sql(@"
            -- Moderator получает права на модерацию
            INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""AssignedAt"", ""AssignedBy"")
            SELECT
                '44444444-4444-4444-4444-444444444444',
                p.""Id"",
                NOW(),
                '00000000-0000-0000-0000-000000000001'
            FROM ""Permissions"" p
            WHERE p.""Name"" IN ('User.View', 'Goal.View', 'Goal.Edit', 'Challenge.View')
            ON CONFLICT (""RoleId"", ""PermissionId"") DO NOTHING;

            -- Premium получает дополнительные права
            INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"", ""AssignedAt"", ""AssignedBy"")
            SELECT
                '55555555-5555-5555-5555-555555555555',
                p.""Id"",
                NOW(),
                '00000000-0000-0000-0000-000000000001'
            FROM ""Permissions"" p
            WHERE p.""Name"" IN ('Goal.Create', 'Challenge.Create', 'User.Edit')
            ON CONFLICT (""RoleId"", ""PermissionId"") DO NOTHING;
        ");

        // 5. Добавляем дополнительные permissions (если нужно)
        Execute.Sql(@"
            INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"", ""Category"", ""CreatedAt"") VALUES
            ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'Moderation.Content', 'Moderate user content', 'Moderation', NOW()),
            ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'Premium.Features', 'Access premium features', 'Premium', NOW()),
            ('ffffffff-ffff-ffff-ffff-ffffffffffff', 'Export.Data', 'Export user data', 'Data', NOW())
            ON CONFLICT (""Name"") DO NOTHING;
        ");

        // 6. Добавляем тестовые UserClaims
        Execute.Sql(@"
            INSERT INTO ""UserClaims"" (""Id"", ""UserId"", ""ClaimType"", ""ClaimValue"", ""CreatedAt"") VALUES
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'SubscriptionLevel', 'Enterprise', NOW()),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', 'SubscriptionLevel', 'Basic', NOW()),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000003', 'SubscriptionLevel', 'Premium', NOW()),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Department', 'IT', NOW()),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', 'Department', 'Sales', NOW())
            ON CONFLICT DO NOTHING;
        ");

        // 7. Добавляем тестовые UserSessions
        Execute.Sql(@"
            INSERT INTO ""UserSessions"" (""Id"", ""UserId"", ""RefreshTokenHash"", ""CreatedAt"", ""ExpiresAt"", ""DeviceInfo"", ""IpAddress"", ""IsRevoked"") VALUES
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000001',
             'hashed_token_admin_1', NOW() - INTERVAL '1 hour', NOW() + INTERVAL '29 days',
             'Chrome/Windows', '192.168.1.100', false),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000002',
             'hashed_token_user_1', NOW() - INTERVAL '2 hours', NOW() + INTERVAL '14 days',
             'Safari/iOS', '192.168.1.101', false),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000003',
             'hashed_token_john_1', NOW() - INTERVAL '3 days', NOW() + INTERVAL '11 days',
             'Firefox/Linux', '192.168.1.102', false)
            ON CONFLICT DO NOTHING;
        ");
    }

    public override void Down()
    {
        // Откат seed данных
        Execute.Sql(@"DELETE FROM ""UserSessions"" WHERE ""UserId"" IN (
            '00000000-0000-0000-0000-000000000001',
            '00000000-0000-0000-0000-000000000002',
            '00000000-0000-0000-0000-000000000003'
        )");

        Execute.Sql(@"DELETE FROM ""UserClaims"" WHERE ""UserId"" IN (
            '00000000-0000-0000-0000-000000000001',
            '00000000-0000-0000-0000-000000000002',
            '00000000-0000-0000-0000-000000000003'
        )");

        Execute.Sql(@"DELETE FROM ""Permissions"" WHERE ""Name"" IN (
            'Moderation.Content', 'Premium.Features', 'Export.Data'
        )");

        Execute.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""RoleId"" IN (
            '44444444-4444-4444-4444-444444444444',
            '55555555-5555-5555-5555-555555555555'
        )");

        Execute.Sql(@"DELETE FROM ""Roles"" WHERE ""Name"" IN ('Moderator', 'Premium')");

        // Восстанавливаем старые значения соли
        Execute.Sql(@"
            UPDATE ""Users"" SET ""PasswordSalt"" = '' WHERE ""Id"" IN (
                '00000000-0000-0000-0000-000000000001',
                '00000000-0000-0000-0000-000000000002',
                '00000000-0000-0000-0000-000000000003'
            );
        ");
    }
}
