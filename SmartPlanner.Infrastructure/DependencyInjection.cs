using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Interfaces.Repositories;
using SmartPlanner.Application.Interfaces.Repositories;
using SmartPlanner.Infrastructure.Configuration;
using SmartPlanner.Infrastructure.FileStorage;
using SmartPlanner.Infrastructure.Persistence;
using SmartPlanner.Infrastructure.Repositories;

namespace SmartPlanner.Infrastructure;

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ✅ ПРАВИЛЬНАЯ конфигурация
            services.Configure<FileStorageOptions>(options =>
            {
                options.DataDirectory = configuration["FileStorage:DataDirectory"] ?? "Data";
            });

            // File Storage - Singleton
            services.AddSingleton<IFileStorageService, FileStorageService>();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IGoalRepository, GoalRepository>();

            services.AddScoped<IAchievementRepository, AchievementRepository>();

            services.AddScoped<IChallengeRepository, ChallengeRepository>();

            services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();

            // UnitOfWork - Scoped
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
