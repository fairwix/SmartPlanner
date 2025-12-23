using FluentMigrator;

[Migration(0009)]
public class CreateSecurityAuditLog : Migration
{
    public override void Up()
    {
        Create.Table("SecurityAuditLogs")
            .WithColumn("Id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("EventType").AsInt32().NotNullable()
            .WithColumn("UserId").AsGuid().Nullable()
            .WithColumn("Email").AsString(200).Nullable()
            .WithColumn("IpAddress").AsString(45).NotNullable()
            .WithColumn("UserAgent").AsString(1000).Nullable()
            .WithColumn("Success").AsBoolean().NotNullable()
            .WithColumn("Details").AsString().Nullable() // JSON
            .WithColumn("Timestamp").AsDateTime().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.ForeignKey("FK_SecurityAuditLogs_Users")
            .FromTable("SecurityAuditLogs").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.SetNull);

        Create.Index("IX_SecurityAuditLogs_EventType")
            .OnTable("SecurityAuditLogs")
            .OnColumn("EventType");

        Create.Index("IX_SecurityAuditLogs_UserId_Timestamp")
            .OnTable("SecurityAuditLogs")
            .OnColumn("UserId").Ascending()
            .OnColumn("Timestamp").Descending();

        Create.Index("IX_SecurityAuditLogs_Timestamp")
            .OnTable("SecurityAuditLogs")
            .OnColumn("Timestamp").Descending();

        Create.Index("IX_SecurityAuditLogs_IpAddress")
            .OnTable("SecurityAuditLogs")
            .OnColumn("IpAddress");

        Create.Index("IX_SecurityAuditLogs_Success")
            .OnTable("SecurityAuditLogs")
            .OnColumn("Success");

        Execute.Sql(@"
            -- Создаем функцию для автоматической очистки
            CREATE OR REPLACE FUNCTION cleanup_old_security_logs()
            RETURNS void AS $$
            BEGIN
                DELETE FROM ""SecurityAuditLogs""
                WHERE ""Timestamp"" < NOW() - INTERVAL '90 days';
            END;
            $$ LANGUAGE plpgsql;

            -- Создаем задание очистки (если используете pg_cron)
            -- SELECT cron.schedule('cleanup-security-logs', '0 2 * * *',
            --   'SELECT cleanup_old_security_logs()');
        ");
    }

    public override void Down()
    {

        Delete.ForeignKey("FK_SecurityAuditLogs_Users").OnTable("SecurityAuditLogs");

        Delete.Index("IX_SecurityAuditLogs_EventType").OnTable("SecurityAuditLogs");
        Delete.Index("IX_SecurityAuditLogs_UserId_Timestamp").OnTable("SecurityAuditLogs");
        Delete.Index("IX_SecurityAuditLogs_Timestamp").OnTable("SecurityAuditLogs");
        Delete.Index("IX_SecurityAuditLogs_IpAddress").OnTable("SecurityAuditLogs");
        Delete.Index("IX_SecurityAuditLogs_Success").OnTable("SecurityAuditLogs");

        Delete.Table("SecurityAuditLogs");

        Execute.Sql("DROP FUNCTION IF EXISTS cleanup_old_security_logs();");
    }
}
