// SmartPlanner.Application.Tests/Services/AttachmentServiceTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Services;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Data;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class AttachmentServiceTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ILogger<AttachmentService>> _mockLogger;
        private readonly AttachmentService _attachmentService;
        private readonly List<FileMetadata> _fileMetadataList;
        private readonly List<MessageAttachment> _messageAttachmentList;
        private readonly List<PostAttachment> _postAttachmentList;
        private readonly List<ProductImage> _productImageList;
        private readonly Guid _testUserId = Guid.NewGuid();

        public AttachmentServiceTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockFileService = new Mock<IFileService>();
            _mockLogger = new Mock<ILogger<AttachmentService>>();

            // Настройка моков для DbSet
            _fileMetadataList = new List<FileMetadata>();
            _messageAttachmentList = new List<MessageAttachment>();
            _postAttachmentList = new List<PostAttachment>();
            _productImageList = new List<ProductImage>();

            var mockFileMetadataSet = CreateMockDbSet(_fileMetadataList);
            var mockMessageAttachmentSet = CreateMockDbSet(_messageAttachmentList);
            var mockPostAttachmentSet = CreateMockDbSet(_postAttachmentList);
            var mockProductImageSet = CreateMockDbSet(_productImageList);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileMetadataSet.Object);
            _mockContext.Setup(c => c.MessageAttachments).Returns(mockMessageAttachmentSet.Object);
            _mockContext.Setup(c => c.PostAttachments).Returns(mockPostAttachmentSet.Object);
            _mockContext.Setup(c => c.ProductImages).Returns(mockProductImageSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _attachmentService = new AttachmentService(
                _mockContext.Object,
                _mockFileService.Object,
                _mockLogger.Object);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T, CancellationToken>((entity, _) => sourceList.Add(entity))
                .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>>(
                    Task.FromResult((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>)null!)));

            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(sourceList.Add);
            mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(entity => sourceList.Remove(entity));

            // Setup for Include extension
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);

            return mockSet;
        }

        [Fact]
        public async Task ValidateAndGetAttachmentsAsync_WithValidFiles_ReturnsFiles()
        {
            // Arrange
            var file1 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var file2 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            _fileMetadataList.AddRange(new[] { file1, file2 });

            var fileIds = new List<Guid> { file1.Id, file2.Id };

            // Act
            var result = await _attachmentService.ValidateAndGetAttachmentsAsync(fileIds, _testUserId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(file1, result);
            Assert.Contains(file2, result);
        }

        [Fact]
        public async Task ValidateAndGetAttachmentsAsync_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var fileIds = new List<Guid>();

            // Act
            var result = await _attachmentService.ValidateAndGetAttachmentsAsync(fileIds, _testUserId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ValidateAndGetAttachmentsAsync_WithUnauthorizedFile_ThrowsException()
        {
            // Arrange
            var authorizedFile = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var unauthorizedFile = new FileMetadata { Id = Guid.NewGuid(), UploadedById = Guid.NewGuid() };
            _fileMetadataList.AddRange(new[] { authorizedFile, unauthorizedFile });

            var fileIds = new List<Guid> { authorizedFile.Id, unauthorizedFile.Id };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _attachmentService.ValidateAndGetAttachmentsAsync(fileIds, _testUserId));
        }

        [Fact]
        public async Task AttachFilesToMessageAsync_WithFiles_CreatesAttachments()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var file1 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var file2 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            _fileMetadataList.AddRange(new[] { file1, file2 });

            var fileIds = new List<Guid> { file1.Id, file2.Id };

            // Act
            await _attachmentService.AttachFilesToMessageAsync(messageId, fileIds, _testUserId);

            // Assert
            Assert.Equal(2, _messageAttachmentList.Count);
            Assert.All(_messageAttachmentList, a => Assert.Equal(messageId, a.MessageId));
            Assert.Equal(0, _messageAttachmentList[0].Order);
            Assert.Equal(1, _messageAttachmentList[1].Order);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AttachFilesToPostAsync_WithCoverImage_SetsCoverCorrectly()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var file1 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var file2 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            _fileMetadataList.AddRange(new[] { file1, file2 });

            var fileIds = new List<Guid> { file1.Id, file2.Id };

            // Act
            await _attachmentService.AttachFilesToPostAsync(postId, fileIds, _testUserId, file1.Id);

            // Assert
            Assert.Equal(2, _postAttachmentList.Count);
            Assert.True(_postAttachmentList[0].IsCover);
            Assert.False(_postAttachmentList[1].IsCover);
        }

        [Fact]
        public async Task GetMessageAttachmentsAsync_ReturnsAttachmentsInOrder()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var file = new FileMetadata
            {
                Id = Guid.NewGuid(),
                OriginalFileName = "test.jpg",
                ContentType = "image/jpeg",
                Size = 1024
            };

            var attachment1 = new MessageAttachment
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                File = file,
                Order = 2
            };

            var attachment2 = new MessageAttachment
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                File = file,
                Order = 1
            };

            _messageAttachmentList.AddRange(new[] { attachment1, attachment2 });

            // Act
            var result = await _attachmentService.GetMessageAttachmentsAsync(messageId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Order); // Should be sorted by Order
            Assert.Equal(2, result[1].Order);
        }

        [Fact]
        public async Task RemoveAttachmentAsync_MessageAttachment_RemovesCorrectly()
        {
            // Arrange
            var file = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var attachment = new MessageAttachment
            {
                Id = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                File = file,
                FileId = file.Id
            };

            _messageAttachmentList.Add(attachment);
            _fileMetadataList.Add(file);

            // Act
            await _attachmentService.RemoveAttachmentAsync(file.Id, _testUserId);

            // Assert
            Assert.Empty(_messageAttachmentList);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveAttachmentAsync_WithDeleteFile_DeletesFile()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var file = new FileMetadata { Id = fileId, UploadedById = _testUserId };
            var attachment = new MessageAttachment
            {
                Id = Guid.NewGuid(),
                MessageId = Guid.NewGuid(),
                File = file,
                FileId = fileId
            };

            _messageAttachmentList.Add(attachment);
            _fileMetadataList.Add(file);

            // Act
            await _attachmentService.RemoveAttachmentAsync(fileId, _testUserId, deleteFile: true);

            // Assert
            _mockFileService.Verify(f => f.DeleteFileAsync(fileId, _testUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductImagesOrderAsync_UpdatesOrdersCorrectly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var file1 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var file2 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };

            var image1 = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                File = file1,
                FileId = file1.Id,
                Order = 0
            };

            var image2 = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                File = file2,
                FileId = file2.Id,
                Order = 1
            };

            _productImageList.AddRange(new[] { image1, image2 });
            _fileMetadataList.AddRange(new[] { file1, file2 });

            var newOrders = new Dictionary<Guid, int>
            {
                [file1.Id] = 5,
                [file2.Id] = 3
            };

            // Act
            await _attachmentService.UpdateProductImagesOrderAsync(productId, newOrders, _testUserId);

            // Assert
            Assert.Equal(5, image1.Order);
            Assert.Equal(3, image2.Order);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveAttachmentAsync_PostCoverImage_AssignsNewCover()
        {
            // Arrange
            var file1 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };
            var file2 = new FileMetadata { Id = Guid.NewGuid(), UploadedById = _testUserId };

            var attachment1 = new PostAttachment
            {
                Id = Guid.NewGuid(),
                PostId = Guid.NewGuid(),
                File = file1,
                FileId = file1.Id,
                IsCover = true,
                Order = 0
            };

            var attachment2 = new PostAttachment
            {
                Id = Guid.NewGuid(),
                PostId = attachment1.PostId,
                File = file2,
                FileId = file2.Id,
                IsCover = false,
                Order = 1
            };

            _postAttachmentList.AddRange(new[] { attachment1, attachment2 });
            _fileMetadataList.AddRange(new[] { file1, file2 });

            // Act
            await _attachmentService.RemoveAttachmentAsync(file1.Id, _testUserId);

            // Assert
            Assert.False(attachment1.IsCover); // attachment1 should be removed
            Assert.True(attachment2.IsCover); // attachment2 should become new cover
        }
    }
}
