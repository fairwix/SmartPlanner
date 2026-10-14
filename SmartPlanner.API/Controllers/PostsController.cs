// SmartPlanner.API/Controllers/PostsController.cs
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
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            IPostService postService,
            IAttachmentService attachmentService,
            ILogger<PostsController> logger)
        {
            _postService = postService;
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<PostWithAttachmentsDto>> CreatePost(
            [FromBody] CreatePostWithAttachmentsDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                // Создаем пост
                var post = await _postService.CreatePostAsync(request, userId);

                // Прикрепляем изображения
                if (request.ImageIds != null && request.ImageIds.Any())
                {
                    await _attachmentService.AttachFilesToPostAsync(
                        post.Id,
                        request.ImageIds,
                        userId,
                        request.CoverImageId);
                }

                // Получаем пост с вложениями
                var result = await _postService.GetPostAsync(post.Id, userId);
                var attachments = await _attachmentService.GetPostAttachmentsAsync(post.Id);

                var response = new PostWithAttachmentsDto
                {
                    Id = result.Id,
                    Title = result.Title,
                    Content = result.Content,
                    AuthorId = result.AuthorId,
                    CreatedAt = result.CreatedAt,
                    PublishedAt = result.PublishedAt,
                    Attachments = attachments
                };

                return CreatedAtAction(nameof(GetPost), new { id = post.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post with attachments");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PostWithAttachmentsDto>> GetPost(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var post = await _postService.GetPostAsync(id, userId);
                var attachments = await _attachmentService.GetPostAttachmentsAsync(id);

                var response = new PostWithAttachmentsDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorId = post.AuthorId,
                    CreatedAt = post.CreatedAt,
                    PublishedAt = post.PublishedAt,
                    LikesCount = post.LikesCount,
                    CommentsCount = post.CommentsCount,
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
                _logger.LogError(ex, "Error getting post {PostId}", id);
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

    public class PostWithAttachmentsDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
    }
}
