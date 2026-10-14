using Microsoft.AspNetCore.Http;

namespace SmartPlanner.Application.Dtos.Files
{
    public class ChunkedUploadDto
    {
        public IFormFile Chunk { get; set; }
        public string UploadId { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public string FileName { get; set; }
        public bool? IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class ChunkedUploadProgressDto
    {
        public string UploadId { get; set; }
        public double Progress { get; set; }
        public int ChunksReceived { get; set; }
        public int TotalChunks { get; set; }
        public string Status { get; set; } // "uploading", "assembling", "completed"
    }
    // Добавляем новые DTO
    public class ChunkedUploadStartDto
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public int TotalChunks { get; set; }
        public string FileHash { get; set; } // Хеш полного файла (вычисляется на клиенте)
        public bool? IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class ChunkedUploadProgressResponseDto : ChunkedUploadProgressDto
    {
        public bool IsDuplicate { get; set; }
        public Guid? ExistingFileId { get; set; }
        public string? Message { get; set; }
    }

    public class CheckDuplicateRequestDto
    {
        public string FileHash { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
    }

    public class CheckDuplicateResponseDto
    {
        public bool IsDuplicate { get; set; }
        public Guid? ExistingFileId { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public DateTime? UploadedAt { get; set; }
    }
}
