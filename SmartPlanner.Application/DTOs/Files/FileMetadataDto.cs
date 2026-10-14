using System;

namespace SmartPlanner.Application.Dtos.Files
{
    public class FileMetadataDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long Size { get; set; }
        public string Path { get; set; } = null!;
        public string? Hash { get; set; }
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int DownloadCount { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public Guid UploadedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        public string? Url => $"/files/{Id}";
        public string? ThumbnailUrl => IsImage() ? $"/files/{Id}/thumbnail" : null;

        private bool IsImage() => ContentType.StartsWith("image/");
    }
}
