using SmartPlanner.Application.Auth.Interfaces;

namespace SmartPlanner.Application.Auth.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string hash = BCrypt.Net.BCrypt.HashPassword(password, salt);

            return (hash, salt);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
