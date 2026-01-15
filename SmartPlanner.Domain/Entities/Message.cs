// SmartPlanner.Domain/Entities/Message.cs
namespace SmartPlanner.Domain.Entities
{
    public class Message : BaseEntity
    {
        public string Content { get; set; } = string.Empty;
        public Guid SenderId { get; set; }
        public Guid ChatId { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }

        // Навигационные свойства
        public virtual User Sender { get; set; } = null!;
        public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
