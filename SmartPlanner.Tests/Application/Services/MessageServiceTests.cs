using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Services;
using SmartPlanner.Application.Tests.Auth.Commands;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class MessageServiceTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<MessageService>> _mockLogger;
        private readonly MessageService _service;
        private readonly Mock<DbSet<Message>> _mockMessagesSet;

        public MessageServiceTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<MessageService>>();
            _service = new MessageService(_mockContext.Object, _mockLogger.Object);
            _mockMessagesSet = new Mock<DbSet<Message>>();

            _mockContext.Setup(c => c.Messages).Returns(_mockMessagesSet.Object);
        }

        [Fact]
        public async Task CreateMessageAsync_ValidDto_CreatesMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var chatId = Guid.NewGuid();
            var replyToMessageId = Guid.NewGuid();
            var dto = new CreateMessageDto
            {
                Content = "Hello World!",
                ChatId = chatId,
                ReplyToMessageId = replyToMessageId
            };

            // Message? capturedMessage = null;
            // _mockMessagesSet.Setup(s => s.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            //     .Callback<Message, CancellationToken>((message, _) => capturedMessage = message)
            //     .Returns(ValueTask.FromResult((object?)null));

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateMessageAsync(dto, userId);

            // Assert
            // Assert.NotNull(result);
            // Assert.Equal(dto.Content, capturedMessage?.Content);
            // Assert.Equal(dto.ChatId, capturedMessage?.ChatId);
            // Assert.Equal(dto.ReplyToMessageId, capturedMessage?.ReplyToMessageId);
            // Assert.Equal(userId, capturedMessage?.SenderId);
            // Assert.False(capturedMessage?.IsEdited);
            // Assert.False(capturedMessage?.IsDeleted);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message") && v.ToString()!.Contains("created")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task CreateMessageAsync_WithoutReply_CreatesMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var chatId = Guid.NewGuid();
            var dto = new CreateMessageDto
            {
                Content = "New message",
                ChatId = chatId,
                ReplyToMessageId = null
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateMessageAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ReplyToMessageId);
        }

        [Fact]
        public async Task GetMessageAsync_MessageExistsAndUserIsSender_ReturnsMessage()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var message = new Message
            {
                Id = messageId,
                Content = "Test Message",
                SenderId = userId
            };

            var messages = new List<Message> { message }.AsQueryable();
            SetupMockDbSet(_mockMessagesSet, messages);

            // Act
            var result = await _service.GetMessageAsync(messageId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(messageId, result.Id);
            Assert.Equal("Test Message", result.Content);
        }

        [Fact]
        public async Task GetMessageAsync_MessageNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var messages = new List<Message>().AsQueryable();
            SetupMockDbSet(_mockMessagesSet, messages);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetMessageAsync(messageId, userId));

            Assert.Contains($"Message {messageId} not found", exception.Message);
        }

        [Fact]
        public async Task GetMessageAsync_UserIsNotSender_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var message = new Message
            {
                Id = messageId,
                Content = "Private Message",
                SenderId = senderId
            };

            var messages = new List<Message> { message }.AsQueryable();
            SetupMockDbSet(_mockMessagesSet, messages);

            // Настраиваем проверку доступа
            var accessCheckMessages = new List<Message>().AsQueryable();
            var mockAccessCheckSet = new Mock<DbSet<Message>>();
            SetupMockDbSet(mockAccessCheckSet, accessCheckMessages);

            _mockContext.SetupSequence(c => c.Messages)
                .Returns(_mockMessagesSet.Object)
                .Returns(mockAccessCheckSet.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetMessageAsync(messageId, otherUserId));

            Assert.Contains("No access to this message", exception.Message);
        }

        [Fact]
        public async Task GetMessageAsync_IncludesAttachmentsAndFiles()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();

            var message = new Message
            {
                Id = messageId,
                Content = "Message with attachment",
                SenderId = userId,
                Attachments = new List<MessageAttachment>
                {
                    new MessageAttachment
                    {
                        FileId = fileId,
                        File = new FileMetadata
                        {
                            Id = fileId,
                            OriginalFileName = "document.pdf",
                            ContentType = "application/pdf"
                        }
                    }
                }
            };

            var messages = new List<Message> { message }.AsQueryable();
            SetupMockDbSet(_mockMessagesSet, messages);

            // Настраиваем проверку доступа
            var accessCheckMessages = new List<Message> { message }.AsQueryable();
            var mockAccessCheckSet = new Mock<DbSet<Message>>();
            SetupMockDbSet(mockAccessCheckSet, accessCheckMessages);

            _mockContext.SetupSequence(c => c.Messages)
                .Returns(_mockMessagesSet.Object)
                .Returns(mockAccessCheckSet.Object);

            // Act
            var result = await _service.GetMessageAsync(messageId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Attachments);
            Assert.Single(result.Attachments);
            Assert.Equal(fileId, result.Attachments.First().FileId);
            Assert.NotNull(result.Attachments.First().File);
            Assert.Equal("document.pdf", result.Attachments.First().File.OriginalFileName);
        }

        [Fact]
        public async Task CheckUserAccessAsync_UserIsSender_ReturnsTrue()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var message = new Message
            {
                Id = messageId,
                SenderId = userId
            };

            var messages = new List<Message> { message }.AsQueryable();
            var mockSet = new Mock<DbSet<Message>>();
            SetupMockDbSet(mockSet, messages);

            _mockContext.Setup(c => c.Messages).Returns(mockSet.Object);

            // Act
            var result = await _service.CheckUserAccessAsync(messageId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckUserAccessAsync_UserIsNotSender_ReturnsFalse()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var message = new Message
            {
                Id = messageId,
                SenderId = senderId
            };

            var messages = new List<Message> { message }.AsQueryable();
            var mockSet = new Mock<DbSet<Message>>();
            SetupMockDbSet(mockSet, messages);

            _mockContext.Setup(c => c.Messages).Returns(mockSet.Object);

            // Act
            var result = await _service.CheckUserAccessAsync(messageId, otherUserId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserAccessAsync_MessageDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var messages = new List<Message>().AsQueryable();
            var mockSet = new Mock<DbSet<Message>>();
            SetupMockDbSet(mockSet, messages);

            _mockContext.Setup(c => c.Messages).Returns(mockSet.Object);

            // Act
            var result = await _service.CheckUserAccessAsync(messageId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateMessageAsync_EmptyContent_CreatesMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateMessageDto
            {
                Content = "",
                ChatId = Guid.NewGuid(),
                ReplyToMessageId = null
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateMessageAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.Content);
        }

        [Fact]
        public async Task CreateMessageAsync_LongContent_CreatesMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var longContent = new string('A', 5000);
            var dto = new CreateMessageDto
            {
                Content = longContent,
                ChatId = Guid.NewGuid(),
                ReplyToMessageId = Guid.NewGuid()
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateMessageAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(longContent, result.Content);
        }

        [Fact]
        public async Task CreateMessageAsync_DeletedMessageAsReply_CreatesMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var replyToMessageId = Guid.NewGuid();
            var dto = new CreateMessageDto
            {
                Content = "Reply to deleted message",
                ChatId = Guid.NewGuid(),
                ReplyToMessageId = replyToMessageId
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateMessageAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(replyToMessageId, result.ReplyToMessageId);
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
