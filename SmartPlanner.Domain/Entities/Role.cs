// SmartPlanner.Domain/Entities/Role.cs
using System;
using System.Collections.Generic;

namespace SmartPlanner.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty; // Инициализация
        public string NormalizedName { get; set; } = string.Empty; // Инициализация
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        //навигационное свойство
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
