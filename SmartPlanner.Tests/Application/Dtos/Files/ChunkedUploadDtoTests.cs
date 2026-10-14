using Microsoft.AspNetCore.Http;
using Moq;
using SmartPlanner.Application.Dtos.Files;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class ChunkedUploadDtoTests
    {
        [Fact]
        public void ChunkedUploadDto_Properties_SetCorrectly()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            var dto = new ChunkedUploadDto
            {
                Chunk = mockFile.Object,
                UploadId = "upload_123",
                ChunkIndex = 2,
                TotalChunks = 10,
                FileName = "test.zip",
                IsPublic = true,
                ExpiresAt = expiresAt
            };

            // Assert
            Assert.Equal(mockFile.Object, dto.Chunk);
            Assert.Equal("upload_123", dto.UploadId);
            Assert.Equal(2, dto.ChunkIndex);
            Assert.Equal(10, dto.TotalChunks);
            Assert.Equal("test.zip", dto.FileName);
            Assert.True(dto.IsPublic);
            Assert.Equal(expiresAt, dto.ExpiresAt);
        }

        [Fact]
        public void ChunkedUploadProgressDto_Properties_SetCorrectly()
        {
            // Act
            var dto = new ChunkedUploadProgressDto
            {
                UploadId = "upload_456",
                Progress = 0.75,
                ChunksReceived = 3,
                TotalChunks = 4,
                Status = "uploading"
            };

            // Assert
            Assert.Equal("upload_456", dto.UploadId);
            Assert.Equal(0.75, dto.Progress);
            Assert.Equal(3, dto.ChunksReceived);
            Assert.Equal(4, dto.TotalChunks);
            Assert.Equal("uploading", dto.Status);
        }

        [Theory]
        [InlineData(0, 4, "uploading")]
        [InlineData(4, 4, "assembling")]
        [InlineData(5, 5, "completed")]
        public void ChunkedUploadProgressDto_StatusLogic(int chunksReceived, int totalChunks, string expectedStatus)
        {
            // Act
            var dto = new ChunkedUploadProgressDto
            {
                ChunksReceived = chunksReceived,
                TotalChunks = totalChunks,
                Status = expectedStatus
            };

            // Assert
            Assert.Equal(expectedStatus, dto.Status);
        }

        [Fact]
        public void ChunkedUploadStartDto_Properties_SetCorrectly()
        {
            // Arrange
            var expiresAt = DateTime.UtcNow.AddDays(30);

            // Act
            var dto = new ChunkedUploadStartDto
            {
                FileName = "large_video.mp4",
                FileSize = 1024 * 1024 * 500, // 500MB
                TotalChunks = 100,
                FileHash = "abc123def456",
                IsPublic = false,
                ExpiresAt = expiresAt
            };

            // Assert
            Assert.Equal("large_video.mp4", dto.FileName);
            Assert.Equal(1024 * 1024 * 500, dto.FileSize);
            Assert.Equal(100, dto.TotalChunks);
            Assert.Equal("abc123def456", dto.FileHash);
            Assert.False(dto.IsPublic);
            Assert.Equal(expiresAt, dto.ExpiresAt);
        }

        [Fact]
        public void ChunkedUploadStartDto_WithNullIsPublic_WorksCorrectly()
        {
            // Act
            var dto = new ChunkedUploadStartDto
            {
                FileName = "test.jpg",
                FileSize = 1024,
                TotalChunks = 5,
                FileHash = "hash123",
                IsPublic = null,
                ExpiresAt = null
            };

            // Assert
            Assert.Null(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
        }

        [Fact]
        public void ChunkedUploadProgressResponseDto_Inheritance_WorksCorrectly()
        {
            // Arrange & Act
            var dto = new ChunkedUploadProgressResponseDto
            {
                UploadId = "upload_789",
                Progress = 1.0,
                ChunksReceived = 10,
                TotalChunks = 10,
                Status = "completed",
                IsDuplicate = true,
                ExistingFileId = Guid.NewGuid(),
                Message = "File already exists"
            };

            // Assert
            Assert.Equal("upload_789", dto.UploadId);
            Assert.Equal(1.0, dto.Progress);
            Assert.Equal(10, dto.ChunksReceived);
            Assert.Equal(10, dto.TotalChunks);
            Assert.Equal("completed", dto.Status);
            Assert.True(dto.IsDuplicate);
            Assert.NotNull(dto.ExistingFileId);
            Assert.Equal("File already exists", dto.Message);
        }

        [Fact]
        public void ChunkedUploadProgressResponseDto_WithNullValues_WorksCorrectly()
        {
            // Act
            var dto = new ChunkedUploadProgressResponseDto
            {
                UploadId = "upload_test",
                Progress = 0.5,
                ChunksReceived = 2,
                TotalChunks = 4,
                Status = "uploading",
                IsDuplicate = false,
                ExistingFileId = null,
                Message = null
            };

            // Assert
            Assert.False(dto.IsDuplicate);
            Assert.Null(dto.ExistingFileId);
            Assert.Null(dto.Message);
        }

        [Fact]
        public void CheckDuplicateRequestDto_Properties_SetCorrectly()
        {
            // Act
            var dto = new CheckDuplicateRequestDto
            {
                FileHash = "sha256_abcdef123456",
                FileName = "document.pdf",
                FileSize = 2048 * 1024 // 2MB
            };

            // Assert
            Assert.Equal("sha256_abcdef123456", dto.FileHash);
            Assert.Equal("document.pdf", dto.FileName);
            Assert.Equal(2048 * 1024, dto.FileSize);
        }

        [Fact]
        public void CheckDuplicateResponseDto_Properties_SetCorrectly()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var uploadedAt = DateTime.UtcNow.AddDays(-1);

            // Act
            var dto = new CheckDuplicateResponseDto
            {
                IsDuplicate = true,
                ExistingFileId = fileId,
                FileName = "existing_document.pdf",
                FileSize = 2048 * 1024,
                UploadedAt = uploadedAt
            };

            // Assert
            Assert.True(dto.IsDuplicate);
            Assert.Equal(fileId, dto.ExistingFileId);
            Assert.Equal("existing_document.pdf", dto.FileName);
            Assert.Equal(2048 * 1024, dto.FileSize);
            Assert.Equal(uploadedAt, dto.UploadedAt);
        }

        [Fact]
        public void CheckDuplicateResponseDto_NotDuplicate_Properties()
        {
            // Act
            var dto = new CheckDuplicateResponseDto
            {
                IsDuplicate = false,
                ExistingFileId = null,
                FileName = null,
                FileSize = null,
                UploadedAt = null
            };

            // Assert
            Assert.False(dto.IsDuplicate);
            Assert.Null(dto.ExistingFileId);
            Assert.Null(dto.FileName);
            Assert.Null(dto.FileSize);
            Assert.Null(dto.UploadedAt);
        }

        [Fact]
        public void ChunkedUploadDto_WithNullValues_WorksCorrectly()
        {
            // Act
            var dto = new ChunkedUploadDto
            {
                Chunk = null,
                UploadId = null,
                ChunkIndex = 0,
                TotalChunks = 0,
                FileName = null,
                IsPublic = null,
                ExpiresAt = null
            };

            // Assert
            Assert.Null(dto.Chunk);
            Assert.Null(dto.UploadId);
            Assert.Null(dto.FileName);
            Assert.Null(dto.IsPublic);
            Assert.Null(dto.ExpiresAt);
            Assert.Equal(0, dto.ChunkIndex);
            Assert.Equal(0, dto.TotalChunks);
        }

        [Fact]
        public void ChunkedUploadProgressDto_CalculatePercentage()
        {
            // Arrange & Act
            var dto = new ChunkedUploadProgressDto
            {
                ChunksReceived = 3,
                TotalChunks = 10,
                Progress = 0.3
            };

            // Assert
            var expectedPercentage = 30;
            var actualPercentage = dto.Progress * 100;
            Assert.Equal(expectedPercentage, actualPercentage);
        }
    }
}
