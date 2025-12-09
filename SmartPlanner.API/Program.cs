using System;
using System.Diagnostics;
using SmartPlanner.Application;
using SmartPlanner.Infrastructure;
using SmartPlanner.API.Configuration;
using FluentMigrator.Runner;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration);

    var app = builder.Build();

    // ========== МИГРАЦИИ ==========
    try
    {
        app.Logger.LogInformation("Checking database migrations...");

        using var scope = app.Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (migrationRunner.HasMigrationsToApplyUp())
        {
            app.Logger.LogInformation("Applying database migrations...");
            migrationRunner.MigrateUp();
            app.Logger.LogInformation("✅ Database migrations applied successfully");
        }
        else
        {
            app.Logger.LogInformation("✅ No pending migrations");
        }
    }
    catch (Exception ex)
    {
        // ЛОГИРУЕМ И ДЕЛАЕМ THROW
        app.Logger.LogCritical(ex, "❌ Database migration failed");
        throw; // "роняем" приложение
    }

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Planner API");
        c.RoutePrefix = "";
    });

    app.UseHttpsRedirection();
    app.MapControllers();

    app.MapGet("/", () => "Smart Planner API - Go to /swagger");
    app.MapGet("/health", () => "OK");

    app.Logger.LogInformation("=== SmartPlanner API started ===");
    app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    // Этот catch ловит ВСЕ ошибки, даже те что ДО настройки логгера

    // Пытаемся записать в лог если возможно
    try
    {
        // Создаем временный логгер
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogCritical(ex, "❌❌❌ APPLICATION STARTUP FAILED: {Message}", ex.Message);

        // Также пишем в консоль (на всякий случай)
        Console.Error.WriteLine($"❌ FATAL ERROR: {ex.Message}");
        Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
    }
    catch
    {
        // Если даже логгер не работает - пишем куда можем
        Console.Error.WriteLine($"❌ FATAL ERROR (logging failed): {ex.Message}");
        Debug.WriteLine($"FATAL: {ex}");
    }

    //как в примере преподавателя
    Environment.ExitCode = -1;
    throw; // пробрасываем дальше
}
