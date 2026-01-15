// SmartPlanner.Domain/Entities/PostAttachment.cs
namespace SmartPlanner.Domain.Entities
{
    public class PostAttachment : BaseEntity
    {
        public Guid PostId { get; set; }
        public Guid FileId { get; set; }
        public int Order { get; set; }
        public bool IsCover { get; set; }

        // Навигационные свойства
        public virtual Post Post { get; set; } = null!;
        public virtual FileMetadata File { get; set; } = null!;

        public PostAttachment()
        {
            Order = 0;
            IsCover = false;
        }
    }
}
