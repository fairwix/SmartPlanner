
namespace SmartPlanner.Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = string.Empty;       // "CanEditGoal"
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;   // "Goals", "Users", "Admin"

        // Навигационное свойство для связи с ролями
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
