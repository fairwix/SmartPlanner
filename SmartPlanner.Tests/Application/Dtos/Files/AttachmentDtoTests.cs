using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class AttachmentDtoTests
    {
        [Fact]
        public void AttachmentDto_Properties_SetCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var dto = new AttachmentDto
            {
                Id = id,
                FileName = "test.jpg",
                ContentType = "image/jpeg",
                Size = 1024,
                FileType = "image",
                Url = "/api/files/test-id",
                ThumbnailUrl = "/api/files/test-id/thumbnail",
                Order = 1,
                IsMain = true,
                IsCover = false,
                AltText = "Test image",
                Width = 800,
                Height = 600
            };

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal("test.jpg", dto.FileName);
            Assert.Equal("image/jpeg", dto.ContentType);
            Assert.Equal(1024, dto.Size);
            Assert.Equal("image", dto.FileType);
            Assert.Equal("/api/files/test-id", dto.Url);
            Assert.Equal("/api/files/test-id/thumbnail", dto.ThumbnailUrl);
            Assert.Equal(1, dto.Order);
            Assert.True(dto.IsMain);
            Assert.False(dto.IsCover);
            Assert.Equal("Test image", dto.AltText);
            Assert.Equal(800, dto.Width);
            Assert.Equal(600, dto.Height);
        }

        [Fact]
        public void FromFileMetadata_CreatesCorrectDto()
        {
            // Arrange
            var file = new FileMetadata
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "photo.jpg",
                ContentType = "image/jpeg",
                Size = 2048,
                Width = 1920,
                Height = 1080
            };

            // Act
            var dto = AttachmentDto.FromFileMetadata(
                file,
                order: 2,
                isMain: true,
                isCover: false,
                altText: "Profile photo");

            // Assert
            Assert.Equal(file.Id, dto.Id);
            Assert.Equal("photo.jpg", dto.FileName);
            Assert.Equal("image/jpeg", dto.ContentType);
            Assert.Equal(2048, dto.Size);
            Assert.Equal("image", dto.FileType);
            Assert.Equal($"/api/files/{file.Id}", dto.Url);
            Assert.Equal($"/api/files/{file.Id}/thumbnail", dto.ThumbnailUrl);
            Assert.Equal(2, dto.Order);
            Assert.True(dto.IsMain);
            Assert.False(dto.IsCover);
            Assert.Equal("Profile photo", dto.AltText);
            Assert.Equal(1920, dto.Width);
            Assert.Equal(1080, dto.Height);
        }

        [Theory]
        [InlineData("image/jpeg", "photo.jpg", "image")]
        [InlineData("image/png", "image.png", "image")]
        [InlineData("video/mp4", "video.mp4", "video")]
        [InlineData("audio/mpeg", "audio.mp3", "audio")]
        [InlineData("application/pdf", "document.pdf", "pdf")]
        [InlineData("application/msword", "document.doc", "document")]
        [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "document.docx", "document")]
        [InlineData("application/vnd.ms-excel", "spreadsheet.xls", "spreadsheet")]
        [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "spreadsheet.xlsx", "spreadsheet")]
        [InlineData("application/vnd.ms-powerpoint", "presentation.ppt", "presentation")]
        [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", "presentation.pptx", "presentation")]
        [InlineData("application/zip", "archive.zip", "archive")]
        [InlineData("application/x-rar-compressed", "archive.rar", "archive")]
        [InlineData("text/plain", "text.txt", "file")]
        [InlineData("application/octet-stream", "unknown.bin", "file")]
        public void GetFileType_ReturnsCorrectType(string contentType, string fileName, string expectedType)
        {
            // Act
            var file = new FileMetadata
            {
                OriginalFileName = fileName,
                ContentType = contentType
            };

            var dto = AttachmentDto.FromFileMetadata(file);

            // Assert
            Assert.Equal(expectedType, dto.FileType);
        }

        [Fact]
        public void FromFileMetadata_WithNullAltText_WorksCorrectly()
        {
            // Arrange
            var file = new FileMetadata
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "test.jpg",
                ContentType = "image/jpeg",
                Size = 1024
            };

            // Act
            var dto = AttachmentDto.FromFileMetadata(file, altText: null);

            // Assert
            Assert.Null(dto.AltText);
        }

        [Fact]
        public void FromFileMetadata_WithNullDimensions_WorksCorrectly()
        {
            // Arrange
            var file = new FileMetadata
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "test.jpg",
                ContentType = "image/jpeg",
                Size = 1024,
                Width = null,
                Height = null
            };

            // Act
            var dto = AttachmentDto.FromFileMetadata(file);

            // Assert
            Assert.Null(dto.Width);
            Assert.Null(dto.Height);
        }

        [Fact]
        public void AttachmentDto_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new AttachmentDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.Id);
            Assert.Equal(string.Empty, dto.FileName);
            Assert.Equal(string.Empty, dto.ContentType);
            Assert.Equal(0, dto.Size);
            Assert.Equal(string.Empty, dto.FileType);
            Assert.Equal(string.Empty, dto.Url);
            Assert.Equal(string.Empty, dto.ThumbnailUrl);
            Assert.Equal(0, dto.Order);
            Assert.False(dto.IsMain);
            Assert.False(dto.IsCover);
            Assert.Null(dto.AltText);
            Assert.Null(dto.Width);
            Assert.Null(dto.Height);
        }
    }
}
