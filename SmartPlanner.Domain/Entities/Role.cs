// SmartPlanner.Domain/Entities/Role.cs
using System;
using System.Collections.Generic;

namespace SmartPlanner.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Инициализация
        public string NormalizedName { get; set; } = string.Empty; // Инициализация
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
