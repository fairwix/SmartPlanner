// SmartPlanner.Domain/Entities/MessageAttachment.cs
namespace SmartPlanner.Domain.Entities
{
    public class MessageAttachment : BaseEntity
    {
        public Guid MessageId { get; set; }
        public Guid FileId { get; set; }
        public int Order { get; set; }
        public DateTime AttachedAt { get; set; }

        // Навигационные свойства
        public virtual Message Message { get; set; } = null!;
        public virtual FileMetadata File { get; set; } = null!;

        public MessageAttachment()
        {
            AttachedAt = DateTime.UtcNow;
            Order = 0;
        }
    }
}
