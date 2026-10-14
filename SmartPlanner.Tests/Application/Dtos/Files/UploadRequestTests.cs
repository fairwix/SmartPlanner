// SmartPlanner.Application.Tests/Dtos/Files/UploadRequestTests.cs
using Microsoft.AspNetCore.Http;
using Moq;
using SmartPlanner.Application.Dtos.Files;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class UploadRequestTests
    {
        [Fact]
        public void UploadFileRequest_Properties_SetCorrectly()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var expiresAt = DateTime.UtcNow.AddDays(30);

            // Act
            var dto = new UploadFileRequest
            {
                File = mockFile.Object,
                IsPublic = true,
                ExpiresAt = expiresAt
            };

            // Assert
            Assert.Equal(mockFile.Object, dto.File);
            Assert.True(dto.IsPublic);
            Assert.Equal(expiresAt, dto.ExpiresAt);
        }

        [Fact]
        public void UploadFileRequest_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new UploadFileRequest();

            // Assert
            Assert.Null(dto.File); // Will be null by default
            Assert.False(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
        }

        [Fact]
        public void UploadFilesRequest_Properties_SetCorrectly()
        {
            // Arrange
            var mockFile1 = new Mock<IFormFile>();
            var mockFile2 = new Mock<IFormFile>();
            var files = new List<IFormFile> { mockFile1.Object, mockFile2.Object };
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var dto = new UploadFilesRequest
            {
                Files = files,
                IsPublic = false,
                ExpiresAt = expiresAt
            };

            // Assert
            Assert.Equal(files, dto.Files);
            Assert.Equal(2, dto.Files.Count);
            Assert.False(dto.IsPublic);
            Assert.Equal(expiresAt, dto.ExpiresAt);
        }

        [Fact]
        public void UploadFilesRequest_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new UploadFilesRequest();

            // Assert
            Assert.NotNull(dto.Files);
            Assert.Empty(dto.Files);
            Assert.False(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
        }

        [Fact]
        public void UploadFilesRequest_WithEmptyList_WorksCorrectly()
        {
            // Act
            var dto = new UploadFilesRequest
            {
                Files = new List<IFormFile>(),
                IsPublic = true,
                ExpiresAt = null
            };

            // Assert
            Assert.Empty(dto.Files);
            Assert.True(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
        }

        [Fact]
        public void UploadFileRequest_WithNullExpiresAt_WorksCorrectly()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            // Act
            var dto = new UploadFileRequest
            {
                File = mockFile.Object,
                IsPublic = false,
                ExpiresAt = null
            };

            // Assert
            Assert.False(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
        }

        [Fact]
        public void UploadFilesRequest_CanAddFiles()
        {
            // Arrange
            var dto = new UploadFilesRequest();
            var mockFile = new Mock<IFormFile>();

            // Act
            dto.Files.Add(mockFile.Object);
            dto.Files.Add(mockFile.Object);

            // Assert
            Assert.Equal(2, dto.Files.Count);
        }
    }
}
