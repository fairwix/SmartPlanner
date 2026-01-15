// SmartPlanner.Application.Tests/Dtos/Files/FilePreviewDtoTests.cs
using SmartPlanner.Application.Dtos.Files;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class FilePreviewDtoTests
    {
        [Fact]
        public void FilePreviewDto_Properties_SetCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var dto = new FilePreviewDto
            {
                Id = id,
                FileName = "preview.jpg",
                ContentType = "image/jpeg",
                Size = 1024,
                FileType = "image",
                Url = "/api/files/preview",
                ThumbnailUrl = "/api/files/preview/thumbnail",
                Width = 800,
                Height = 600,
                AltText = "Preview image"
            };

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal("preview.jpg", dto.FileName);
            Assert.Equal("image/jpeg", dto.ContentType);
            Assert.Equal(1024, dto.Size);
            Assert.Equal("image", dto.FileType);
            Assert.Equal("/api/files/preview", dto.Url);
            Assert.Equal("/api/files/preview/thumbnail", dto.ThumbnailUrl);
            Assert.Equal(800, dto.Width);
            Assert.Equal(600, dto.Height);
            Assert.Equal("Preview image", dto.AltText);
        }

        [Fact]
        public void FromFileMetadataDto_CreatesCorrectPreview()
        {
            // Arrange
            var fileDto = new FileMetadataDto
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "document.pdf",
                ContentType = "application/pdf",
                Size = 2048 * 1024,
                Width = null,
                Height = null
            };

            // Act
            var previewDto = FilePreviewDto.FromFileMetadata(fileDto);

            // Assert
            Assert.Equal(fileDto.Id, previewDto.Id);
            Assert.Equal("document.pdf", previewDto.FileName);
            Assert.Equal("application/pdf", previewDto.ContentType);
            Assert.Equal(2048 * 1024, previewDto.Size);
            Assert.Equal("pdf", previewDto.FileType);
            Assert.Equal($"/api/files/{fileDto.Id}", previewDto.Url);
            Assert.Equal($"/api/files/{fileDto.Id}/thumbnail?size=small", previewDto.ThumbnailUrl);
            Assert.Null(previewDto.Width);
            Assert.Null(previewDto.Height);
            Assert.Null(previewDto.AltText);
        }

        [Theory]
        [InlineData("image/jpeg", "photo.jpg", "image")]
        [InlineData("image/png", "screenshot.png", "image")]
        [InlineData("video/mp4", "movie.mp4", "video")]
        [InlineData("audio/mpeg", "song.mp3", "audio")]
        [InlineData("application/pdf", "document.pdf", "pdf")]
        [InlineData("application/msword", "doc.doc", "document")]
        [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "doc.docx", "document")]
        [InlineData("application/vnd.ms-excel", "data.xls", "spreadsheet")]
        [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "data.xlsx", "spreadsheet")]
        [InlineData("application/vnd.ms-powerpoint", "presentation.ppt", "presentation")]
        [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", "presentation.pptx", "presentation")]
        [InlineData("text/plain", "readme.txt", "file")]
        [InlineData("application/json", "config.json", "file")]
        public void GetFileType_ReturnsCorrectType(string contentType, string fileName, string expectedType)
        {
            // Arrange
            var fileDto = new FileMetadataDto
            {
                OriginalFileName = fileName,
                ContentType = contentType
            };

            // Act
            var previewDto = FilePreviewDto.FromFileMetadata(fileDto);

            // Assert
            Assert.Equal(expectedType, previewDto.FileType);
        }

        [Fact]
        public void FromFileMetadataDto_WithImageDimensions_PreservesDimensions()
        {
            // Arrange
            var fileDto = new FileMetadataDto
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                Size = 1024,
                Width = 1920,
                Height = 1080
            };

            // Act
            var previewDto = FilePreviewDto.FromFileMetadata(fileDto);

            // Assert
            Assert.Equal(1920, previewDto.Width);
            Assert.Equal(1080, previewDto.Height);
        }

        [Fact]
        public void FilePreviewDto_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new FilePreviewDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.Id);
            Assert.Null(dto.FileName);
            Assert.Null(dto.ContentType);
            Assert.Equal(0, dto.Size);
            Assert.Null(dto.FileType);
            Assert.Null(dto.Url);
            Assert.Null(dto.ThumbnailUrl);
            Assert.Null(dto.Width);
            Assert.Null(dto.Height);
            Assert.Null(dto.AltText);
        }

        [Fact]
        public void ThumbnailUrl_IncludesSizeParameter()
        {
            // Arrange
            var fileDto = new FileMetadataDto
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "test.jpg",
                ContentType = "image/jpeg"
            };

            // Act
            var previewDto = FilePreviewDto.FromFileMetadata(fileDto);

            // Assert
            Assert.Contains("?size=small", previewDto.ThumbnailUrl);
            Assert.Equal($"/api/files/{fileDto.Id}/thumbnail?size=small", previewDto.ThumbnailUrl);
        }
    }
}
