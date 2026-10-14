using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;

namespace SmartPlanner.Infrastructure;

public static class MigrationRunner
{
    public static void RunMigrations(string connectionString, IServiceProvider serviceProvider)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connectionStringBuilder.Database ?? "smartplanner_db";

        EnsureDatabaseCreated(connectionString, databaseName!);

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("MigrationRunner");

        var hasPendingMigrations = runner.HasMigrationsToApplyUp();

        if (hasPendingMigrations)
        {
            logger.LogInformation("Applying database migrations...");
            runner.MigrateUp();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations to apply");
        }
    }

    private static void EnsureDatabaseCreated(string connectionString, string databaseName)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };

        using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", connection);
        var exists = cmd.ExecuteScalar() != null;

        if (!exists)
        {
            using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
            createCmd.ExecuteNonQuery();
        }
    }
}
