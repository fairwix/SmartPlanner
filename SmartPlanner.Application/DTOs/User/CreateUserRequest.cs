using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPlanner.Application.DTOs.User;

    public class CreateUserRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; init; } = string.Empty;

        public List<string> Interests { get; init; } = new List<string>();
    }

    public class UpdateUserRequest
    {
        [StringLength(50, MinimumLength = 3)]
        public string? Username { get; init; }

        public List<string>? Interests { get; init; }
    }

    public class UserResponse
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public List<string> Interests { get; init; } = new List<string>();
        public int Balance { get; init; }
        public int StreakCount { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastLogin { get; init; }
    }

