using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPlanner.Domain.DTOs.User
{
    public class CreateUserRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public List<string> Interests { get; set; } = new List<string>();
    }

    public class UpdateUserRequest
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; set; }

        public List<string>? Interests { get; set; }
    }

    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = new List<string>();
        public int Balance { get; set; }
        public int StreakCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
    }
}