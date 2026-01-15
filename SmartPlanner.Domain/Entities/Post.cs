// SmartPlanner.Domain/Entities/Post.cs
namespace SmartPlanner.Domain.Entities
{
    public class Post : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }

        // Навигационные свойства
        public virtual User Author { get; set; } = null!;
        public virtual ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>();
    }
}
