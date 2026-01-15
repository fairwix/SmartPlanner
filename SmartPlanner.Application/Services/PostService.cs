using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files.Requests;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Services
{
    public interface IPostService
    {
        Task<Post> CreatePostAsync(CreatePostWithAttachmentsDto dto, Guid userId);
        Task<Post> GetPostAsync(Guid postId, Guid userId);
        Task<bool> CheckUserAccessAsync(Guid postId, Guid userId);
    }

    public class PostService : IPostService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(IApplicationDbContext context, ILogger<PostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Post> CreatePostAsync(CreatePostWithAttachmentsDto dto, Guid userId)
        {
            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                AuthorId = userId,
                CategoryId = dto.CategoryId,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow
            };

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Post {PostId} created by user {UserId}", post.Id, userId);
            return post;
        }

        public async Task<Post> GetPostAsync(Guid postId, Guid userId)
        {
            var post = await _context.Posts
                .Include(p => p.Attachments)
                .ThenInclude(a => a.File)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                throw new KeyNotFoundException($"Post {postId} not found");

            if (!post.IsPublished && post.AuthorId != userId)
                throw new UnauthorizedAccessException("No access to this post");

            return post;
        }

        public async Task<bool> CheckUserAccessAsync(Guid postId, Guid userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return false;

            return post.AuthorId == userId;
        }
    }
}
