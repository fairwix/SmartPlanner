// SmartPlanner.Application.Dtos.Common/FilePreviewDto.cs
namespace SmartPlanner.Application.Dtos.Files
{
    public class FilePreviewDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public string FileType { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }

        // Для изображений
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? AltText { get; set; }

        public static FilePreviewDto FromFileMetadata(FileMetadataDto file)
        {
            return new FilePreviewDto
            {
                Id = file.Id,
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                Size = file.Size,
                FileType = GetFileType(file.ContentType, file.OriginalFileName),
                Url = $"/api/files/{file.Id}",
                ThumbnailUrl = $"/api/files/{file.Id}/thumbnail?size=small",
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

            return "file";
        }
    }
}
