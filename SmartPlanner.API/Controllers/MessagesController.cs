// SmartPlanner.API/Controllers/MessagesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Services;
using System.Security.Claims;
using SmartPlanner.Application.Dtos.Files.Requests;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMessageService messageService,
            IAttachmentService attachmentService,
            ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<MessageWithAttachmentsDto>> CreateMessage(
            [FromBody] CreateMessageWithAttachmentsDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                // Создаем сообщение
                var messageDto = new CreateMessageDto
                {
                    Content = request.Content,
                    ChatId = request.ChatId,
                    ReplyToMessageId = request.ReplyToMessageId
                };

                var message = await _messageService.CreateMessageAsync(messageDto, userId);

                // Прикрепляем файлы
                if (request.AttachmentIds != null && request.AttachmentIds.Any())
                {
                    await _attachmentService.AttachFilesToMessageAsync(
                        message.Id,
                        request.AttachmentIds,
                        userId);
                }

                // Получаем сообщение с вложениями
                var result = await _messageService.GetMessageAsync(message.Id, userId);
                var attachments = await _attachmentService.GetMessageAttachmentsAsync(message.Id);

                var response = new MessageWithAttachmentsDto
                {
                    Id = result.Id,
                    Content = result.Content,
                    ChatId = result.ChatId,
                    SenderId = result.SenderId,
                    CreatedAt = result.CreatedAt,
                    Attachments = attachments
                };

                return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message with attachments");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MessageWithAttachmentsDto>> GetMessage(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var message = await _messageService.GetMessageAsync(id, userId);
                var attachments = await _attachmentService.GetMessageAttachmentsAsync(id);

                var response = new MessageWithAttachmentsDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    ChatId = message.ChatId,
                    SenderId = message.SenderId,
                    CreatedAt = message.CreatedAt,
                    Attachments = attachments
                };

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message {MessageId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }

    public class MessageWithAttachmentsDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
    }
}
