using System;

namespace SmartPlanner.Domain.Entities
{
    public class FileMetadata : BaseEntity
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!; // Безопасное имя в хранилище
        public string OriginalFileName { get; set; } = null!; // Оригинальное имя пользователя
        public string ContentType { get; set; } = null!; // MIME type
        public long Size { get; set; } // Размер в байтах
        public string Path { get; set; } = null!; // Относительный путь
        public string? Hash { get; set; } // SHA256 для дедупликации
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; } // Время истечения
        public int DownloadCount { get; set; } = 0;

        // Внешние ключи
        public Guid UploadedById { get; set; }

        // Навигационные свойства
        public virtual User UploadedBy { get; set; } = null!;

        public int? Width { get; set; }
        public int? Height { get; set; }

        // Для EXIF данных (опционально)
        public DateTime? DateTaken { get; set; }
        public string CameraModel { get; set; }
        public string Location { get; set; }
        public int? Orientation { get; set; }

        // Для thumbnails
        public string ThumbnailPath { get; set; } // путь к маленькому thumbnail
        public string MediumPath { get; set; }    // путь к среднему thumbnail

        public FileMetadata()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public virtual ICollection<MessageAttachment> MessageAttachments { get; set; } = new List<MessageAttachment>();
        public virtual ICollection<PostAttachment> PostAttachments { get; set; } = new List<PostAttachment>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
