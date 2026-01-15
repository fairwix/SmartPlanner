// SmartPlanner.Application.Tests/Dtos/Files/FileMetadataDtoTests.cs
using SmartPlanner.Application.Dtos.Files;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class FileMetadataDtoTests
    {
        [Fact]
        public void FileMetadataDto_Properties_SetCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-1);
            var updatedAt = DateTime.UtcNow;
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var dto = new FileMetadataDto
            {
                Id = id,
                FileName = "stored_name.jpg",
                OriginalFileName = "original_photo.jpg",
                ContentType = "image/jpeg",
                Size = 2048 * 1024, // 2MB
                Path = "/uploads/2024/01/13/abc123.jpg",
                Hash = "sha256_abcdef",
                IsPublic = true,
                ExpiresAt = expiresAt,
                DownloadCount = 15,
                Width = 1920,
                Height = 1080,
                UploadedById = userId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Equal("stored_name.jpg", dto.FileName);
            Assert.Equal("original_photo.jpg", dto.OriginalFileName);
            Assert.Equal("image/jpeg", dto.ContentType);
            Assert.Equal(2048 * 1024, dto.Size);
            Assert.Equal("/uploads/2024/01/13/abc123.jpg", dto.Path);
            Assert.Equal("sha256_abcdef", dto.Hash);
            Assert.True(dto.IsPublic);
            Assert.Equal(expiresAt, dto.ExpiresAt);
            Assert.Equal(15, dto.DownloadCount);
            Assert.Equal(1920, dto.Width);
            Assert.Equal(1080, dto.Height);
            Assert.Equal(userId, dto.UploadedById);
            Assert.Equal(createdAt, dto.CreatedAt);
            Assert.Equal(updatedAt, dto.UpdatedAt);
        }

        [Fact]
        public void Url_Property_ReturnsCorrectUrl()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new FileMetadataDto { Id = id };

            // Act & Assert
            Assert.Equal($"/files/{id}", dto.Url);
        }

        [Theory]
        [InlineData("image/jpeg", true)]
        [InlineData("image/png", true)]
        [InlineData("image/gif", true)]
        [InlineData("image/webp", true)]
        [InlineData("application/pdf", false)]
        [InlineData("text/plain", false)]
        [InlineData("video/mp4", false)]
        public void ThumbnailUrl_Property_ReturnsCorrectValue(string contentType, bool shouldHaveThumbnail)
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new FileMetadataDto
            {
                Id = id,
                ContentType = contentType
            };

            // Act & Assert
            if (shouldHaveThumbnail)
            {
                Assert.Equal($"/files/{id}/thumbnail", dto.ThumbnailUrl);
            }
            else
            {
                Assert.Null(dto.ThumbnailUrl);
            }
        }

        [Fact]
        public void FileMetadataDto_NullableProperties_WorkCorrectly()
        {
            // Act
            var dto = new FileMetadataDto
            {
                Hash = null,
                ExpiresAt = null,
                Width = null,
                Height = null
            };

            // Assert
            Assert.Null(dto.Hash);
            Assert.Null(dto.ExpiresAt);
            Assert.Null(dto.Width);
            Assert.Null(dto.Height);
        }

        [Fact]
        public void FileMetadataDto_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new FileMetadataDto();

            // Assert
            Assert.Equal(Guid.Empty, dto.Id);
            Assert.Null(dto.FileName);
            Assert.Null(dto.OriginalFileName);
            Assert.Null(dto.ContentType);
            Assert.Equal(0, dto.Size);
            Assert.Null(dto.Path);
            Assert.Null(dto.Hash);
            Assert.False(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
            Assert.Equal(0, dto.DownloadCount);
            Assert.Null(dto.Width);
            Assert.Null(dto.Height);
            Assert.Equal(Guid.Empty, dto.UploadedById);
            Assert.Equal(default, dto.CreatedAt);
            Assert.Equal(default, dto.UpdatedAt);
        }

        [Fact]
        public void IsImage_Method_ReturnsCorrectValue()
        {
            // Этот тест проверяет приватный метод через свойство ThumbnailUrl
            // Arrange
            var imageDto = new FileMetadataDto { ContentType = "image/jpeg" };
            var nonImageDto = new FileMetadataDto { ContentType = "application/pdf" };

            // Act & Assert
            Assert.NotNull(imageDto.ThumbnailUrl); // ThumbnailUrl будет иметь значение для изображений
            Assert.Null(nonImageDto.ThumbnailUrl); // ThumbnailUrl будет null для не-изображений
        }
    }
}
