using Microsoft.AspNetCore.Http;

namespace SmartPlanner.Application.Dtos.Files
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; } = null!;
        public bool IsPublic { get; set; } = false;
        public DateTime? ExpiresAt { get; set; }
    }
}
