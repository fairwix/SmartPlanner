using Microsoft.AspNetCore.Http;

namespace SmartPlanner.Application.Dtos.Files
{
    public class UploadFilesRequest
    {
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
        public bool IsPublic { get; set; } = false;
        public DateTime? ExpiresAt { get; set; }
    }
}
