// SmartPlanner.Application/Auth/Services/PasswordHasher.cs
using SmartPlanner.Application.Auth.Interfaces;

namespace SmartPlanner.Application.Auth.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            // Используем BCrypt как в ТЗ
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string hash = BCrypt.Net.BCrypt.HashPassword(password, salt);

            // BCrypt включает соль в hash, но возвращаем отдельно для совместимости
            return (hash, salt);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            // BCrypt автоматически извлекает соль из hash
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
