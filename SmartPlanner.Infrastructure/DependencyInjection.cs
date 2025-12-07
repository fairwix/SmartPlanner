using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Interfaces.Services; // Будет добавлен позже
using SmartPlanner.Infrastructure.Data;
using SmartPlanner.Infrastructure.Services;

namespace SmartPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Настройка DbContext с PostgreSQL
        var connectionString = configuration.GetConnectionString("PostgreSQL")
                               ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");

        // DbContext БЕЗ EF Core миграций (используем FluentMigrator)
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    // НЕ включаем миграции EF Core - используем FluentMigrator
                }));

        // Регистрация DbContext как IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ✅ РЕГИСТРИРУЕМ СПЕЦИАЛИЗИРОВАННЫЕ СЕРВИСЫ (не Generic Repository)
        services.AddScoped<IAchievementCheckerService, AchievementCheckerService>();
        services.AddScoped<AI.ChallengeRecommendationService>();

        // ========== FLUENTMIGRATOR ==========
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()  // PostgreSQL
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(DependencyInjection).Assembly).For.Migrations()
                .ScanIn(typeof(DependencyInjection).Assembly).For.EmbeddedResources())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }
}
