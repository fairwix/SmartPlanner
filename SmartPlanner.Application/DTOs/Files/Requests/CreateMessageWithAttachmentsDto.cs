// SmartPlanner.Application/Dtos/Files/Requests/CreateMessageWithAttachmentsDto.cs

using Microsoft.AspNetCore.Http;

namespace SmartPlanner.Application.Dtos.Files.Requests
{
    public class CreateMessageWithAttachmentsDto
    {
        public string Content { get; set; } = string.Empty;
        public Guid ChatId { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public List<Guid> AttachmentIds { get; set; } = new();
    }

    public class CreatePostWithAttachmentsDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public List<Guid> ImageIds { get; set; } = new();
        public Guid? CoverImageId { get; set; }
    }

    public class UploadProductImageDto
    {
        public IFormFile File { get; set; } = null!;
        public bool IsMain { get; set; }
        public string? AltText { get; set; }
    }

    public class UpdateImagesOrderDto
    {
        public Dictionary<Guid, int> ImageOrders { get; set; } = new();
    }
}
