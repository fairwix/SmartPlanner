using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Models;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Application.Services;
using SmartPlanner.Infrastructure.Data;

namespace SmartPlanner.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Настройка DbContext с PostgreSQL
            var connectionString = configuration.GetConnectionString("PostgreSQL")
                                   ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");

            // DbContext БЕЗ EF Core миграций
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    }));

            // Регистрация DbContext как IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // КОНФИГУРАЦИЯ НАСТРОЕК - УПРОЩЕННЫЙ ВАРИАНТ
            // Просто передаем IConfigurationSection - это работает в ASP.NET Core

            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<CorsSettings>(configuration.GetSection("Cors"));
            services.Configure<RateLimitSettings>(configuration.GetSection("RateLimiting"));

            // ✅ Регистрируем специализированные сервисы
            services.AddScoped<IAchievementCheckerService, AchievementCheckerService>();
            services.AddScoped<IConfirmationTokenService, ConfirmationTokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuditService, AuditService>();

            // Фоновые сервисы
            services.AddHostedService<AuditLogCleanupService>();
            services.AddHostedService<EmailTokenCleanupService>();

            // IWebHostEnvironment (для EmailService)
            // services.AddSingleton<IHostEnvironment>(sp =>
            //     sp.GetRequiredService<IHostEnvironment>());

            // ========== FLUENTMIGRATOR ==========
            /*services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(DependencyInjection).Assembly).For.Migrations()
                    .ScanIn(typeof(DependencyInjection).Assembly).For.EmbeddedResources())
                .AddLogging(lb => lb.AddFluentMigratorConsole());*/

            return services;
        }
    }
}
