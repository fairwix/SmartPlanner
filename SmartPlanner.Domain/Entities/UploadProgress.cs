namespace SmartPlanner.Domain.Entities
{
    public class UploadProgress : BaseEntity
    {
        public string UploadId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public int UploadedChunks { get; set; }
        public string? FileHash { get; set; } // Хеш для проверки дубликатов
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = "uploading"; // uploading, assembling, completed, failed
    }
}
