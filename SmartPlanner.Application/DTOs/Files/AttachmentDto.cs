// SmartPlanner.Application/Dtos/Files/AttachmentDto.cs

using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Dtos.Files
{
    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string FileType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsMain { get; set; }
        public bool IsCover { get; set; }
        public string? AltText { get; set; }

        // Для превью
        public int? Width { get; set; }
        public int? Height { get; set; }

        public static AttachmentDto FromFileMetadata(
            FileMetadata file,
            int order = 0,
            bool isMain = false,
            bool isCover = false,
            string? altText = null)
        {
            return new AttachmentDto
            {
                Id = file.Id,
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                Size = file.Size,
                FileType = GetFileType(file.ContentType, file.OriginalFileName),
                Url = $"/api/files/{file.Id}",
                ThumbnailUrl = $"/api/files/{file.Id}/thumbnail",
                Order = order,
                IsMain = isMain,
                IsCover = isCover,
                AltText = altText,
                Width = file.Width,
                Height = file.Height
            };
        }

        private static string GetFileType(string contentType, string fileName)
        {
            if (contentType.StartsWith("image/")) return "image";
            if (contentType.StartsWith("video/")) return "video";
            if (contentType.StartsWith("audio/")) return "audio";
            if (contentType == "application/pdf") return "pdf";
            if (contentType.Contains("word") || fileName.EndsWith(".doc") || fileName.EndsWith(".docx"))
                return "document";
            if (contentType.Contains("excel") || fileName.EndsWith(".xls") || fileName.EndsWith(".xlsx"))
                return "spreadsheet";
            if (contentType.Contains("presentation") || fileName.EndsWith(".ppt") || fileName.EndsWith(".pptx"))
                return "presentation";
            if (contentType.Contains("zip") || fileName.EndsWith(".zip") || fileName.EndsWith(".rar"))
                return "archive";

            return "file";
        }
    }
}
