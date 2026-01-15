using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Domain.Tests.Entities
{
    public class MessageTests
    {
        [Fact]
        public void Message_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var message = new Message();

            // Assert
            Assert.Equal(string.Empty, message.Content);
            Assert.Equal(Guid.Empty, message.SenderId);
            Assert.Equal(Guid.Empty, message.ChatId);
            Assert.Null(message.ReplyToMessageId);
            Assert.False(message.IsEdited);
            Assert.False(message.IsDeleted);
            Assert.NotNull(message.Attachments);
            Assert.Empty(message.Attachments);
            Assert.Null(message.Sender);
        }

        [Fact]
        public void Message_Properties_CanBeSet()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var chatId = Guid.NewGuid();
            var replyToId = Guid.NewGuid();
            var user = new User { Id = senderId, Email = "test@example.com" };

            // Act
            var message = new Message
            {
                Id = messageId,
                Content = "Hello World!",
                SenderId = senderId,
                ChatId = chatId,
                ReplyToMessageId = replyToId,
                IsEdited = true,
                IsDeleted = true,
                Sender = user
            };

            // Assert
            Assert.Equal(messageId, message.Id);
            Assert.Equal("Hello World!", message.Content);
            Assert.Equal(senderId, message.SenderId);
            Assert.Equal(chatId, message.ChatId);
            Assert.Equal(replyToId, message.ReplyToMessageId);
            Assert.True(message.IsEdited);
            Assert.True(message.IsDeleted);
            Assert.Equal(user, message.Sender);
        }

        [Fact]
        public void Message_CanAddAttachments()
        {
            // Arrange
            var message = new Message();
            var attachment1 = new MessageAttachment();
            var attachment2 = new MessageAttachment();

            // Act
            message.Attachments.Add(attachment1);
            message.Attachments.Add(attachment2);

            // Assert
            Assert.Equal(2, message.Attachments.Count);
            Assert.Contains(attachment1, message.Attachments);
            Assert.Contains(attachment2, message.Attachments);
        }

        [Fact]
        public void Message_WithNullReplyToMessageId_WorksCorrectly()
        {
            // Arrange & Act
            var message = new Message
            {
                Content = "Test message",
                ReplyToMessageId = null
            };

            // Assert
            Assert.Null(message.ReplyToMessageId);
            Assert.Equal("Test message", message.Content);
        }

        [Fact]
        public void Message_InheritsFromBaseEntity()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                CreatedAt = now,
                UpdatedAt = now
            };

            // Assert
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.Equal(now, message.CreatedAt);
            Assert.Equal(now, message.UpdatedAt);
        }

        [Theory]
        [InlineData("Short")]
        [InlineData("Very long message with multiple words and punctuation!")]
        [InlineData("Message with emoji 😊")]
        [InlineData("")]
        public void Message_Content_CanBeSetToVariousValues(string content)
        {
            // Arrange & Act
            var message = new Message
            {
                Content = content
            };

            // Assert
            Assert.Equal(content, message.Content);
        }
    }
}
