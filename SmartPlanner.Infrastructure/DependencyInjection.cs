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

namespace SmartPlanner.Infrastructure
{
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

            // ✅ ПРАВИЛЬНАЯ регистрация репозиториев с одним параметром string
            services.AddScoped<IUserRepository>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new UserRepository(options.UsersFilePath); // ✅ Только один параметр
            });

            services.AddScoped<IGoalRepository>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new GoalRepository(options.GoalsFilePath); // ✅ Только один параметр
            });

            services.AddScoped<IAchievementRepository>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new AchievementRepository(options.AchievementsFilePath); // ✅ Только один параметр
            });

            services.AddScoped<IChallengeRepository>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new ChallengeRepository(options.ChallengesFilePath); // ✅ Только один параметр
            });

            services.AddScoped<IUserAchievementRepository>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
                return new UserAchievementRepository(options.UserAchievementsFilePath); // ✅ Только один параметр
            });

            // UnitOfWork - Scoped
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            return services;
        }
    }
}