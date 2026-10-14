// SmartPlanner.Domain/Entities/Product.cs
namespace SmartPlanner.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool IsActive { get; set; }
        public int StockQuantity { get; set; }

        // Навигационные свойства
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
