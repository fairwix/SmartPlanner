using Microsoft.AspNetCore.Http;
using Moq;
using SmartPlanner.Application.Dtos.Files.Requests;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files.Requests
{
    public class RequestDtoTests
    {
        [Fact]
        public void CreateMessageWithAttachmentsDto_Properties_SetCorrectly()
        {
            // Arrange
            var chatId = Guid.NewGuid();
            var replyToMessageId = Guid.NewGuid();
            var attachmentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var dto = new CreateMessageWithAttachmentsDto
            {
                Content = "Hello world with attachments",
                ChatId = chatId,
                ReplyToMessageId = replyToMessageId,
                AttachmentIds = attachmentIds
            };

            // Assert
            Assert.Equal("Hello world with attachments", dto.Content);
            Assert.Equal(chatId, dto.ChatId);
            Assert.Equal(replyToMessageId, dto.ReplyToMessageId);
            Assert.Equal(attachmentIds, dto.AttachmentIds);
        }

        [Fact]
        public void CreateMessageWithAttachmentsDto_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new CreateMessageWithAttachmentsDto();

            // Assert
            Assert.Equal(string.Empty, dto.Content);
            Assert.Equal(Guid.Empty, dto.ChatId);
            Assert.Null(dto.ReplyToMessageId);
            Assert.Empty(dto.AttachmentIds);
        }

        [Fact]
        public void CreatePostWithAttachmentsDto_Properties_SetCorrectly()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var imageIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var coverImageId = Guid.NewGuid();

            // Act
            var dto = new CreatePostWithAttachmentsDto
            {
                Title = "My Awesome Post",
                Content = "This is the content of my post",
                CategoryId = categoryId,
                ImageIds = imageIds,
                CoverImageId = coverImageId
            };

            // Assert
            Assert.Equal("My Awesome Post", dto.Title);
            Assert.Equal("This is the content of my post", dto.Content);
            Assert.Equal(categoryId, dto.CategoryId);
            Assert.Equal(imageIds, dto.ImageIds);
            Assert.Equal(coverImageId, dto.CoverImageId);
        }

        [Fact]
        public void CreatePostWithAttachmentsDto_NullableProperties_WorkCorrectly()
        {
            // Act
            var dto = new CreatePostWithAttachmentsDto
            {
                Title = "Test Post",
                Content = "Content",
                CategoryId = null,
                ImageIds = new List<Guid>(),
                CoverImageId = null
            };

            // Assert
            Assert.Null(dto.CategoryId);
            Assert.Null(dto.CoverImageId);
            Assert.Empty(dto.ImageIds);
        }

        [Fact]
        public void UploadProductImageDto_Properties_SetCorrectly()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            // Act
            var dto = new UploadProductImageDto
            {
                File = mockFile.Object,
                IsMain = true,
                AltText = "Product main image"
            };

            // Assert
            Assert.Equal(mockFile.Object, dto.File);
            Assert.True(dto.IsMain);
            Assert.Equal("Product main image", dto.AltText);
        }

        [Fact]
        public void UploadProductImageDto_WithNullAltText_WorksCorrectly()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            // Act
            var dto = new UploadProductImageDto
            {
                File = mockFile.Object,
                IsMain = false,
                AltText = null
            };

            // Assert
            Assert.False(dto.IsMain);
            Assert.Null(dto.AltText);
        }

        [Fact]
        public void UpdateImagesOrderDto_Properties_SetCorrectly()
        {
            // Arrange
            var imageOrders = new Dictionary<Guid, int>
            {
                [Guid.NewGuid()] = 1,
                [Guid.NewGuid()] = 2,
                [Guid.NewGuid()] = 3
            };

            // Act
            var dto = new UpdateImagesOrderDto
            {
                ImageOrders = imageOrders
            };

            // Assert
            Assert.Equal(imageOrders, dto.ImageOrders);
            Assert.Equal(3, dto.ImageOrders.Count);
        }

        [Fact]
        public void UpdateImagesOrderDto_EmptyDictionary_WorksCorrectly()
        {
            // Act
            var dto = new UpdateImagesOrderDto
            {
                ImageOrders = new Dictionary<Guid, int>()
            };

            // Assert
            Assert.Empty(dto.ImageOrders);
        }
    }
}
