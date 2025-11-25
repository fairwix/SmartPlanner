// SmartPlanner.Application/Users/Dtos/UserDto.cs

using SmartPlanner.Application.Common.Dtos;

namespace SmartPlanner.Application.Users.Dtos
{
    public class UserDto : BaseDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();
        public int Balance { get; set; }
        public int StreakCount { get; set; }
        public DateTime LastLogin { get; set; }
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new();
    }

    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public List<string>? Interests { get; set; }
    }
}