using System;
using System.Diagnostics;
using SmartPlanner.Application;
using SmartPlanner.Infrastructure;
using SmartPlanner.API.Configuration;
using FluentMigrator.Runner;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Регистрация сервисов
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApiServices(builder.Configuration);

    var app = builder.Build();

    // Миграции ДО настройки pipeline
    await ApplyMigrationsAsync(app);

    // Middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Planner API");
            c.RoutePrefix = "";
        });
    }

    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapGet("/", () => "Smart Planner API");
    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

    app.Logger.LogInformation("SmartPlanner API started ({Env})",
        app.Environment.EnvironmentName);

    await app.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: {ex}");
    return 1; // exit code для контейнера/systemd
}

return 0;

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (runner.HasMigrationsToApplyUp())
        {
            logger.LogInformation("Applying migrations...");
            runner.MigrateUp();
            logger.LogInformation("✅ Migrations applied");
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Migration failed");
        throw; // роняем приложение
    }
}
