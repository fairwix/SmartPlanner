using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files.Requests;
using SmartPlanner.Application.Services;
using SmartPlanner.Application.UnitTests.Goals.Queries;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class PostServiceTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<PostService>> _mockLogger;
        private readonly PostService _service;
        private readonly Mock<DbSet<Post>> _mockPostsSet;

        public PostServiceTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<PostService>>();
            _service = new PostService(_mockContext.Object, _mockLogger.Object);
            _mockPostsSet = new Mock<DbSet<Post>>();

            _mockContext.Setup(c => c.Posts).Returns(_mockPostsSet.Object);
        }

        [Fact]
        public async Task CreatePostAsync_ValidDto_CreatesPostSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var dto = new CreatePostWithAttachmentsDto
            {
                Title = "Test Post",
                Content = "Test Content",
                CategoryId = categoryId
            };

            // Post? capturedPost = null;
            // _mockPostsSet.Setup(s => s.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()))
            //     .Callback<Post, CancellationToken>((post, _) => capturedPost = post)
            //     .Returns(ValueTask.FromResult((object?)null));
            //
            // _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            //     .ReturnsAsync(1);

            // Act
            var result = await _service.CreatePostAsync(dto, userId);

            // // Assert
            // Assert.NotNull(result);
            // Assert.Equal(dto.Title, capturedPost?.Title);
            // Assert.Equal(dto.Content, capturedPost?.Content);
            // Assert.Equal(dto.CategoryId, capturedPost?.CategoryId);
            // Assert.Equal(userId, capturedPost?.AuthorId);
            // Assert.True(capturedPost?.IsPublished);
            // Assert.NotNull(capturedPost?.PublishedAt);
            // Assert.True(capturedPost?.PublishedAt <= DateTime.UtcNow);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Post") && v.ToString()!.Contains("created")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetPostAsync_PostExistsAndUserIsAuthor_ReturnsPost()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                Title = "Test Post",
                AuthorId = userId,
                IsPublished = true
            };

            var posts = new List<Post> { post }.AsQueryable();
            SetupMockDbSet(_mockPostsSet, posts);

            // Act
            var result = await _service.GetPostAsync(postId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(postId, result.Id);
            Assert.Equal("Test Post", result.Title);
        }

        [Fact]
        public async Task GetPostAsync_PostNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var posts = new List<Post>().AsQueryable();
            SetupMockDbSet(_mockPostsSet, posts);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetPostAsync(postId, userId));

            Assert.Contains($"Post {postId} not found", exception.Message);
        }

        [Fact]
        public async Task GetPostAsync_PostUnpublishedAndUserNotAuthor_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                Title = "Draft Post",
                AuthorId = authorId,
                IsPublished = false,
                PublishedAt = null
            };

            var posts = new List<Post> { post }.AsQueryable();
            SetupMockDbSet(_mockPostsSet, posts);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetPostAsync(postId, otherUserId));

            Assert.Contains("No access to this post", exception.Message);
        }

        [Fact]
        public async Task GetPostAsync_PostUnpublishedButUserIsAuthor_ReturnsPost()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                Title = "My Draft",
                AuthorId = userId,
                IsPublished = false,
                PublishedAt = null
            };

            var posts = new List<Post> { post }.AsQueryable();
            SetupMockDbSet(_mockPostsSet, posts);

            // Act
            var result = await _service.GetPostAsync(postId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(postId, result.Id);
            Assert.Equal("My Draft", result.Title);
        }

        [Fact]
        public async Task GetPostAsync_PostPublishedAndUserNotAuthor_ReturnsPost()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                Title = "Published Post",
                AuthorId = authorId,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-1)
            };

            var posts = new List<Post> { post }.AsQueryable();
            SetupMockDbSet(_mockPostsSet, posts);

            // Act
            var result = await _service.GetPostAsync(postId, otherUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(postId, result.Id);
            Assert.Equal("Published Post", result.Title);
        }

        [Fact]
        public async Task GetPostAsync_IncludesAttachmentsAndFiles()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();

            var post = new Post
            {
                Id = postId,
                AuthorId = userId,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow,
                Attachments = new List<PostAttachment>
                {
                    new PostAttachment
                    {
                        FileId = fileId,
                        File = new FileMetadata
                        {
                            Id = fileId,
                            OriginalFileName = "attachment.jpg",
                            ContentType = "image/jpeg"
                        }
                    }
                }
            };

            var posts = new List<Post> { post }.AsQueryable();
            SetupMockDbSet(_mockPostsSet, posts);

            // Act
            var result = await _service.GetPostAsync(postId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Attachments);
            Assert.Single(result.Attachments);
            Assert.Equal(fileId, result.Attachments.First().FileId);
            Assert.NotNull(result.Attachments.First().File);
            Assert.Equal("attachment.jpg", result.Attachments.First().File.OriginalFileName);
        }

        [Fact]
        public async Task CheckUserAccessAsync_UserIsAuthor_ReturnsTrue()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                AuthorId = userId
            };

            _mockContext.Setup(c => c.Posts.FindAsync(postId))
                .ReturnsAsync(post);

            // Act
            var result = await _service.CheckUserAccessAsync(postId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckUserAccessAsync_UserIsNotAuthor_ReturnsFalse()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                AuthorId = authorId
            };

            _mockContext.Setup(c => c.Posts.FindAsync(postId))
                .ReturnsAsync(post);

            // Act
            var result = await _service.CheckUserAccessAsync(postId, otherUserId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserAccessAsync_PostNotFound_ReturnsFalse()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockContext.Setup(c => c.Posts.FindAsync(postId))
                .ReturnsAsync((Post?)null);

            // Act
            var result = await _service.CheckUserAccessAsync(postId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreatePostAsync_WithNullCategoryId_CreatesPostSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreatePostWithAttachmentsDto
            {
                Title = "Post without category",
                Content = "Content",
                CategoryId = null
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreatePostAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreatePostAsync_EmptyTitle_CreatesPostSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreatePostWithAttachmentsDto
            {
                Title = "",
                Content = "Content",
                CategoryId = Guid.NewGuid()
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreatePostAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.Title);
        }

        [Fact]
        public async Task CreatePostAsync_LongContent_CreatesPostSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var longContent = new string('A', 10000);
            var dto = new CreatePostWithAttachmentsDto
            {
                Title = "Long Post",
                Content = longContent,
                CategoryId = Guid.NewGuid()
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreatePostAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longContent, result.Content);
        }

        private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
        {
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(data.Provider));

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        }
    }
}
