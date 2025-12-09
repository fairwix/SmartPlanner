// SmartPlanner.Domain/Entities/Interest.cs
namespace SmartPlanner.Domain.Entities;

public class Interest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Навигационное свойство
    public virtual List<UserInterest> UserInterests { get; set; } = new();
}
