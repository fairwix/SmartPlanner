// SmartPlanner.Domain/Entities/ProductImage.cs
namespace SmartPlanner.Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Guid FileId { get; set; }
        public bool IsMain { get; set; }
        public int Order { get; set; }
        public string? AltText { get; set; }

        // Навигационные свойства
        public virtual Product Product { get; set; } = null!;
        public virtual FileMetadata File { get; set; } = null!;

        public ProductImage()
        {
            Order = 0;
            IsMain = false;
            AltText = string.Empty;
        }
    }
}
