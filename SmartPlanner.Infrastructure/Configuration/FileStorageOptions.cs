// SmartPlanner.Infrastructure/Configuration/FileStorageOptions.cs
namespace SmartPlanner.Infrastructure.Configuration
{
    public class FileStorageOptions
    {
        public string DataDirectory { get; set; } = "Data";
        
        // Вычисляемые свойства для путей к файлам
        public string UsersFilePath => Path.Combine(DataDirectory, "users.json");
        public string GoalsFilePath => Path.Combine(DataDirectory, "goals.json");
        public string ChallengesFilePath => Path.Combine(DataDirectory, "challenges.json");
        public string AchievementsFilePath => Path.Combine(DataDirectory, "achievements.json");
        public string UserAchievementsFilePath => Path.Combine(DataDirectory, "user-achievements.json");
    }
}