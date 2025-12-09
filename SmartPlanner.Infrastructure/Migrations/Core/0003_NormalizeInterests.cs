using System;
using FluentMigrator;

[Migration(0003)]
public class NormalizeInterests : Migration
{
    public override void Up()
    {
        // 1. Создаем таблицу Interests (если не существует)
        if (!Schema.Table("Interests").Exists())
        {
            Create.Table("Interests")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("Name").AsString(100).NotNullable()
                .WithColumn("Description").AsString(500).Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_Interests_Name")
                .OnTable("Interests")
                .OnColumn("Name")
                .Unique();
        }

        // 2. Создаем таблицу UserInterests (если не существует)
        if (!Schema.Table("UserInterests").Exists())
        {
            Create.Table("UserInterests")
                .WithColumn("Id").AsGuid().PrimaryKey()
                .WithColumn("UserId").AsGuid().NotNullable()
                .WithColumn("InterestId").AsGuid().NotNullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_UserInterests_UserId_InterestId")
                .OnTable("UserInterests")
                .OnColumn("UserId").Ascending()
                .OnColumn("InterestId").Ascending()
                .WithOptions().Unique();

            // Foreign keys
            Create.ForeignKey("FK_UserInterests_Users")
                .FromTable("UserInterests").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_UserInterests_Interests")
                .FromTable("UserInterests").ForeignColumn("InterestId")
                .ToTable("Interests").PrimaryColumn("Id")
                .OnDelete(System.Data.Rule.Cascade);
        }

        // 3. Добавляем популярные интересы
        Execute.Sql(@"
            INSERT INTO ""Interests"" (""Id"", ""Name"", ""Description"") VALUES
            ('11111111-1111-1111-1111-111111111111', 'programming', 'Coding and software development'),
            ('22222222-2222-2222-2222-222222222222', 'sports', 'Physical activities and sports'),
            ('33333333-3333-3333-3333-333333333333', 'reading', 'Books and literature'),
            ('44444444-4444-4444-4444-444444444444', 'music', 'Listening and playing music'),
            ('55555555-5555-5555-5555-555555555555', 'travel', 'Travel and exploration'),
            ('66666666-6666-6666-6666-666666666666', 'gaming', 'Video and board games'),
            ('77777777-7777-7777-7777-777777777777', 'fitness', 'Exercise and wellness'),
            ('88888888-8888-8888-8888-888888888888', 'cooking', 'Culinary arts and cooking'),
            ('99999999-9999-9999-9999-999999999999', 'photography', 'Photography and visual arts'),
            ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'art', 'Drawing, painting and creative arts')
            ON CONFLICT (""Name"") DO NOTHING;
        ");

        // 4. Обновляем seed данных - преобразуем JSON интересы в нормализованные
        // Администратор
        Execute.Sql(@"
            -- Администратор интересы
            INSERT INTO ""UserInterests"" (""Id"", ""UserId"", ""InterestId"") VALUES
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111'),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '22222222-2222-2222-2222-222222222222'),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '33333333-3333-3333-3333-333333333333')
            ON CONFLICT (""UserId"", ""InterestId"") DO NOTHING;
        ");

        // Test user
        Execute.Sql(@"
            -- Test user интересы
            INSERT INTO ""UserInterests"" (""Id"", ""UserId"", ""InterestId"") VALUES
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', '44444444-4444-4444-4444-444444444444'),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', '55555555-5555-5555-5555-555555555555'),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', '66666666-6666-6666-6666-666666666666')
            ON CONFLICT (""UserId"", ""InterestId"") DO NOTHING;
        ");

        // John Doe
        Execute.Sql(@"
            -- John Doe интересы
            INSERT INTO ""UserInterests"" (""Id"", ""UserId"", ""InterestId"") VALUES
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000003', '77777777-7777-7777-7777-777777777777'),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000003', '88888888-8888-8888-8888-888888888888'),
            (gen_random_uuid(), '00000000-0000-0000-0000-000000000003', '99999999-9999-9999-9999-999999999999')
            ON CONFLICT (""UserId"", ""InterestId"") DO NOTHING;
        ");
    }

    public override void Down()
    {
        // Удаляем таблицы (для отката)
        Delete.Table("UserInterests");
        Delete.Table("Interests");
    }
}
