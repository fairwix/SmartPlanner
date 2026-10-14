// SmartPlanner.Application/Services/AttachmentService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Services
{
    public interface IAttachmentService
    {
        Task<List<FileMetadata>> ValidateAndGetAttachmentsAsync(
            List<Guid> fileIds,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task AttachFilesToMessageAsync(
            Guid messageId,
            List<Guid> fileIds,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task AttachFilesToPostAsync(
            Guid postId,
            List<Guid> fileIds,
            Guid userId,
            Guid? coverImageId = null,
            CancellationToken cancellationToken = default);

        Task AttachFilesToProductAsync(
            Guid productId,
            List<Guid> fileIds,
            Guid userId,
            bool isMain = false,
            string? altText = null,
            CancellationToken cancellationToken = default);

        Task<List<AttachmentDto>> GetMessageAttachmentsAsync(
            Guid messageId,
            CancellationToken cancellationToken = default);

        Task<List<AttachmentDto>> GetPostAttachmentsAsync(
            Guid postId,
            CancellationToken cancellationToken = default);

        Task<List<AttachmentDto>> GetProductImagesAsync(
            Guid productId,
            CancellationToken cancellationToken = default);

        Task RemoveAttachmentAsync(
            Guid attachmentId,
            Guid userId,
            bool deleteFile = false,
            CancellationToken cancellationToken = default);

        Task UpdateProductImagesOrderAsync(
            Guid productId,
            Dictionary<Guid, int> imageOrders,
            Guid userId,
            CancellationToken cancellationToken = default);
    }

    public class AttachmentService : IAttachmentService
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IApplicationDbContext context,
            IFileService fileService,
            ILogger<AttachmentService> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<List<FileMetadata>> ValidateAndGetAttachmentsAsync(
            List<Guid> fileIds,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (fileIds == null || fileIds.Count == 0)
                return new List<FileMetadata>();

            var files = await _context.FileMetadata
                .Where(f => fileIds.Contains(f.Id) && f.UploadedById == userId)
                .ToListAsync(cancellationToken);

            if (files.Count != fileIds.Distinct().Count())
            {
                throw new UnauthorizedAccessException("Некоторые файлы не найдены или нет доступа");
            }

            return files;
        }

        public async Task AttachFilesToMessageAsync(
            Guid messageId,
            List<Guid> fileIds,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (fileIds == null || fileIds.Count == 0)
                return;

            var files = await ValidateAndGetAttachmentsAsync(fileIds, userId, cancellationToken);

            var existingAttachments = await _context.MessageAttachments
                .Where(ma => ma.MessageId == messageId)
                .ToListAsync(cancellationToken);

            var maxOrder = existingAttachments.Any()
                ? existingAttachments.Max(ma => ma.Order)
                : -1;

            foreach (var file in files)
            {
                var index = files.IndexOf(file);
                var attachment = new MessageAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    FileId = file.Id,
                    Order = maxOrder + index + 1,
                    AttachedAt = DateTime.UtcNow
                };

                await _context.MessageAttachments.AddAsync(attachment, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Прикреплено {Count} файлов к сообщению {MessageId}", files.Count, messageId);
        }

        public async Task AttachFilesToPostAsync(
            Guid postId,
            List<Guid> fileIds,
            Guid userId,
            Guid? coverImageId = null,
            CancellationToken cancellationToken = default)
        {
            if (fileIds == null || fileIds.Count == 0)
                return;

            var files = await ValidateAndGetAttachmentsAsync(fileIds, userId, cancellationToken);

            var existingAttachments = await _context.PostAttachments
                .Where(pa => pa.PostId == postId)
                .ToListAsync(cancellationToken);

            if (coverImageId.HasValue)
            {
                var oldCover = existingAttachments.FirstOrDefault(pa => pa.IsCover);
                if (oldCover != null)
                {
                    oldCover.IsCover = false;
                    _context.PostAttachments.Update(oldCover);
                }
            }

            var maxOrder = existingAttachments.Any()
                ? existingAttachments.Max(pa => pa.Order)
                : -1;

            foreach (var file in files)
            {
                var index = files.IndexOf(file);
                var attachment = new PostAttachment
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    FileId = file.Id,
                    Order = maxOrder + index + 1,
                    IsCover = coverImageId.HasValue && coverImageId.Value == file.Id
                };

                await _context.PostAttachments.AddAsync(attachment, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Прикреплено {Count} файлов к посту {PostId}", files.Count, postId);
        }

        public async Task AttachFilesToProductAsync(
            Guid productId,
            List<Guid> fileIds,
            Guid userId,
            bool isMain = false,
            string? altText = null,
            CancellationToken cancellationToken = default)
        {
            if (fileIds == null || fileIds.Count == 0)
                return;

            var files = await ValidateAndGetAttachmentsAsync(fileIds, userId, cancellationToken);

            var existingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .ToListAsync(cancellationToken);

            if (isMain)
            {
                var oldMain = existingImages.FirstOrDefault(pi => pi.IsMain);
                if (oldMain != null)
                {
                    oldMain.IsMain = false;
                    _context.ProductImages.Update(oldMain);
                }
            }

            var maxOrder = existingImages.Any()
                ? existingImages.Max(pi => pi.Order)
                : -1;

            foreach (var file in files)
            {
                var index = files.IndexOf(file);
                var image = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    FileId = file.Id,
                    IsMain = isMain && index == 0,
                    Order = maxOrder + index + 1,
                    AltText = altText ?? string.Empty
                };

                await _context.ProductImages.AddAsync(image, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Прикреплено {Count} изображений к продукту {ProductId}", files.Count, productId);
        }

        public async Task<List<AttachmentDto>> GetMessageAttachmentsAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            var attachments = await _context.MessageAttachments
                .Include(ma => ma.File)
                .Where(ma => ma.MessageId == messageId)
                .OrderBy(ma => ma.Order)
                .ToListAsync(cancellationToken);

            return attachments.Select(ma => AttachmentDto.FromFileMetadata(ma.File, ma.Order)).ToList();
        }

        public async Task<List<AttachmentDto>> GetPostAttachmentsAsync(
            Guid postId,
            CancellationToken cancellationToken = default)
        {
            var attachments = await _context.PostAttachments
                .Include(pa => pa.File)
                .Where(pa => pa.PostId == postId)
                .OrderBy(pa => pa.Order)
                .ToListAsync(cancellationToken);

            return attachments.Select(pa =>
                AttachmentDto.FromFileMetadata(pa.File, pa.Order, isCover: pa.IsCover)).ToList();
        }

        public async Task<List<AttachmentDto>> GetProductImagesAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            var images = await _context.ProductImages
                .Include(pi => pi.File)
                .Where(pi => pi.ProductId == productId)
                .OrderBy(pi => pi.Order)
                .ToListAsync(cancellationToken);

            return images.Select(pi =>
                AttachmentDto.FromFileMetadata(pi.File, pi.Order, pi.IsMain, altText: pi.AltText)).ToList();
        }

        public async Task RemoveAttachmentAsync(
            Guid attachmentId,
            Guid userId,
            bool deleteFile = false,
            CancellationToken cancellationToken = default)
        {
            var messageAttachment = await _context.MessageAttachments
                .Include(ma => ma.File)
                .FirstOrDefaultAsync(ma => ma.FileId == attachmentId && ma.File.UploadedById == userId, cancellationToken);

            if (messageAttachment != null)
            {
                _context.MessageAttachments.Remove(messageAttachment);

                if (deleteFile)
                {
                    await _fileService.DeleteFileAsync(attachmentId, userId, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var postAttachment = await _context.PostAttachments
                .Include(pa => pa.File)
                .FirstOrDefaultAsync(pa => pa.FileId == attachmentId && pa.File.UploadedById == userId, cancellationToken);

            if (postAttachment != null)
            {
                if (postAttachment.IsCover)
                {
                    var nextCover = await _context.PostAttachments
                        .Where(x => x.PostId == postAttachment.PostId && x.Id != postAttachment.Id)
                        .OrderBy(x => x.Order)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (nextCover != null)
                    {
                        nextCover.IsCover = true;
                        _context.PostAttachments.Update(nextCover);
                    }
                }

                _context.PostAttachments.Remove(postAttachment);

                if (deleteFile)
                {
                    await _fileService.DeleteFileAsync(attachmentId, userId, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var productImage = await _context.ProductImages
                .Include(pi => pi.File)
                .FirstOrDefaultAsync(pi => pi.FileId == attachmentId && pi.File.UploadedById == userId, cancellationToken);

            if (productImage != null)
            {
                if (productImage.IsMain)
                {
                    var nextMain = await _context.ProductImages
                        .Where(x => x.ProductId == productImage.ProductId && x.Id != productImage.Id)
                        .OrderBy(x => x.Order)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (nextMain != null)
                    {
                        nextMain.IsMain = true;
                        _context.ProductImages.Update(nextMain);
                    }
                }

                _context.ProductImages.Remove(productImage);

                if (deleteFile)
                {
                    await _fileService.DeleteFileAsync(attachmentId, userId, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            throw new FileNotFoundException("Вложение не найдено или нет доступа");
        }

        public async Task UpdateProductImagesOrderAsync(
            Guid productId,
            Dictionary<Guid, int> imageOrders,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var images = await _context.ProductImages
                .Include(pi => pi.File)
                .Where(pi => pi.ProductId == productId && pi.File.UploadedById == userId)
                .ToListAsync(cancellationToken);

            foreach (var image in images)
            {
                if (imageOrders.TryGetValue(image.FileId, out var order))
                {
                    image.Order = order;
                    _context.ProductImages.Update(image);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
