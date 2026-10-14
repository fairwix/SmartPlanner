using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Dtos.Files.Requests;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Services
{
    public interface IMessageService
    {
        Task<Message> CreateMessageAsync(CreateMessageDto dto, Guid userId);
        Task<Message> GetMessageAsync(Guid messageId, Guid userId);
        Task<bool> CheckUserAccessAsync(Guid messageId, Guid userId);
    }

    public class MessageService : IMessageService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<MessageService> _logger;

        public MessageService(IApplicationDbContext context, ILogger<MessageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Message> CreateMessageAsync(CreateMessageDto dto, Guid userId)
        {
            var message = new Message
            {
                Content = dto.Content,
                SenderId = userId,
                ChatId = dto.ChatId,
                ReplyToMessageId = dto.ReplyToMessageId
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Message {MessageId} created by user {UserId}", message.Id, userId);
            return message;
        }

        public async Task<Message> GetMessageAsync(Guid messageId, Guid userId)
        {
            var message = await _context.Messages
                .Include(m => m.Attachments)
                .ThenInclude(a => a.File)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                throw new KeyNotFoundException($"Message {messageId} not found");

            var hasAccess = await _context.Messages
                .AnyAsync(m => m.Id == messageId && m.SenderId == userId);

            if (!hasAccess)
                throw new UnauthorizedAccessException("No access to this message");

            return message;
        }

        public async Task<bool> CheckUserAccessAsync(Guid messageId, Guid userId)
        {
            return await _context.Messages
                .AnyAsync(m => m.Id == messageId && m.SenderId == userId);
        }
    }

    public class CreateMessageDto
    {
        public string Content { get; set; } = string.Empty;
        public Guid ChatId { get; set; }
        public Guid? ReplyToMessageId { get; set; }
    }
}
