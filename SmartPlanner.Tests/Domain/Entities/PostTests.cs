using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Domain.Tests.Entities
{
    public class PostTests
    {
        [Fact]
        public void Post_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var post = new Post();

            // Assert
            Assert.Equal(string.Empty, post.Title);
            Assert.Equal(string.Empty, post.Content);
            Assert.Equal(Guid.Empty, post.AuthorId);
            Assert.Null(post.CategoryId);
            Assert.False(post.IsPublished);
            Assert.Null(post.PublishedAt);
            Assert.Equal(0, post.LikesCount);
            Assert.Equal(0, post.CommentsCount);
            Assert.NotNull(post.Attachments);
            Assert.Empty(post.Attachments);
            Assert.Null(post.Author);
        }

        [Fact]
        public void Post_Properties_CanBeSet()
        {
            // Arrange
            var postId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var publishedAt = DateTime.UtcNow;
            var author = new User { Id = authorId, Email = "author@example.com" };

            // Act
            var post = new Post
            {
                Id = postId,
                Title = "My First Blog Post",
                Content = "This is the content of my blog post.",
                AuthorId = authorId,
                CategoryId = categoryId,
                IsPublished = true,
                PublishedAt = publishedAt,
                LikesCount = 42,
                CommentsCount = 7,
                Author = author
            };

            // Assert
            Assert.Equal(postId, post.Id);
            Assert.Equal("My First Blog Post", post.Title);
            Assert.Equal("This is the content of my blog post.", post.Content);
            Assert.Equal(authorId, post.AuthorId);
            Assert.Equal(categoryId, post.CategoryId);
            Assert.True(post.IsPublished);
            Assert.Equal(publishedAt, post.PublishedAt);
            Assert.Equal(42, post.LikesCount);
            Assert.Equal(7, post.CommentsCount);
            Assert.Equal(author, post.Author);
        }

        [Fact]
        public void Post_CanAddAttachments()
        {
            // Arrange
            var post = new Post();
            var attachment1 = new PostAttachment();
            var attachment2 = new PostAttachment();

            // Act
            post.Attachments.Add(attachment1);
            post.Attachments.Add(attachment2);

            // Assert
            Assert.Equal(2, post.Attachments.Count);
            Assert.Contains(attachment1, post.Attachments);
            Assert.Contains(attachment2, post.Attachments);
        }

        [Fact]
        public void Post_WithNullCategoryId_WorksCorrectly()
        {
            // Arrange & Act
            var post = new Post
            {
                Title = "Uncategorized Post",
                CategoryId = null
            };

            // Assert
            Assert.Null(post.CategoryId);
            Assert.Equal("Uncategorized Post", post.Title);
        }

        [Fact]
        public void Post_WithNullPublishedAt_WhenNotPublished()
        {
            // Arrange & Act
            var post = new Post
            {
                Title = "Draft Post",
                IsPublished = false,
                PublishedAt = null
            };

            // Assert
            Assert.False(post.IsPublished);
            Assert.Null(post.PublishedAt);
        }

        [Fact]
        public void Post_IncrementCounters_WorksCorrectly()
        {
            // Arrange
            var post = new Post();

            // Act
            post.LikesCount = 10;
            post.CommentsCount = 5;

            // Assert
            Assert.Equal(10, post.LikesCount);
            Assert.Equal(5, post.CommentsCount);
        }

        [Theory]
        [InlineData("Short", "Short content")]
        [InlineData("Very Long Title That Might Span Multiple Lines", "Detailed content with many paragraphs...")]
        [InlineData("Title with 😊 emoji", "Content with emoji too 😎")]
        public void Post_TitleAndContent_CanBeSetToVariousValues(string title, string content)
        {
            // Arrange & Act
            var post = new Post
            {
                Title = title,
                Content = content
            };

            // Assert
            Assert.Equal(title, post.Title);
            Assert.Equal(content, post.Content);
        }

        [Fact]
        public void Post_CanBeDraft_WithoutPublishedAt()
        {
            // Arrange & Act
            var post = new Post
            {
                Title = "Draft",
                Content = "This is a draft post",
                IsPublished = false,
                PublishedAt = null
            };

            // Assert
            Assert.False(post.IsPublished);
            Assert.Null(post.PublishedAt);
        }

        [Fact]
        public void Post_CanBePublished_WithPublishedAt()
        {
            // Arrange
            var publishedAt = DateTime.UtcNow;

            // Act
            var post = new Post
            {
                Title = "Published Post",
                Content = "This post is published",
                IsPublished = true,
                PublishedAt = publishedAt
            };

            // Assert
            Assert.True(post.IsPublished);
            Assert.Equal(publishedAt, post.PublishedAt);
        }
    }
}
