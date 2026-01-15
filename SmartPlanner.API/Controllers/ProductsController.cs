// SmartPlanner.API.Controllers.ProductsController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Services;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IAttachmentService _attachmentService;
        private readonly IFileService _fileService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            IAttachmentService attachmentService,
            IFileService fileService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _attachmentService = attachmentService;
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
/// Создать новый продукт
/// </summary>
[HttpPost]
public async Task<ActionResult<ProductResponseDto>> CreateProduct(
    [FromBody] Application.Services.CreateProductDto request)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var product = await _productService.CreateProductAsync(request, userId);

        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            OwnerId = product.OwnerId,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            StockQuantity = product.StockQuantity,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Images = new List<AttachmentDto>()
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при создании продукта");
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "Внутренняя ошибка сервера" });
    }
}

/// <summary>
/// Получить продукт с изображениями
/// </summary>
[HttpGet("{id}")]
[AllowAnonymous]
[ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id)
{
    try
    {
        var userId = GetCurrentUserId(); // Guid.Empty для анонимных
        var product = await _productService.GetProductAsync(id, userId);
        var images = await _attachmentService.GetProductImagesAsync(id);

        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            OwnerId = product.OwnerId,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            StockQuantity = product.StockQuantity,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Images = images
        };

        return Ok(response);
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { error = "Продукт не найден" });
    }
    catch (UnauthorizedAccessException)
    {
        return StatusCode(StatusCodes.Status403Forbidden,
            new { error = "Нет доступа к продукту" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при получении продукта {ProductId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "Внутренняя ошибка сервера" });
    }
}
        /// <summary>
        /// Получить все изображения продукта
        /// </summary>
        [HttpGet("{id}/images")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<AttachmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AttachmentDto>>> GetProductImages(Guid id)
        {
            try
            {
                var images = await _attachmentService.GetProductImagesAsync(id);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении изображений продукта {ProductId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Загрузить и привязать изображение к продукту
        /// </summary>
        [HttpPost("{id}/images")]
        [RequestSizeLimit(50_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AttachmentDto>> UploadProductImage(
            Guid id,
            [FromForm] UploadProductImageDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                // Проверяем права на продукт
                var hasAccess = await _productService.CheckUserAccessAsync(id, userId);
                if (!hasAccess)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "Нет доступа к продукту" });
                }

                // Загружаем файл
                var fileMetadata = await _fileService.UploadFileAsync(
                    request.File,
                    userId,
                    isPublic: true); // Изображения продуктов обычно публичные

                // Привязываем к продукту
                await _attachmentService.AttachFilesToProductAsync(
                    id,
                    new List<Guid> { fileMetadata.Id },
                    userId,
                    isMain: request.IsMain,
                    altText: request.AltText);

                // Получаем информацию о прикреплении
                var images = await _attachmentService.GetProductImagesAsync(id);
                var newImage = images.FirstOrDefault(img => img.Id == fileMetadata.Id);

                return CreatedAtAction(nameof(GetProductImages), new { id }, newImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке изображения для продукта {ProductId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Удалить изображение продукта
        /// </summary>
        [HttpDelete("{productId}/images/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(
            Guid productId,
            Guid imageId,
            [FromQuery] bool deleteFile = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                // Проверяем права на продукт
                var hasAccess = await _productService.CheckUserAccessAsync(productId, userId);
                if (!hasAccess)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "Нет доступа к продукту" });
                }

                await _attachmentService.RemoveAttachmentAsync(
                    imageId,
                    userId,
                    deleteFile);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении изображения {ImageId} продукта {ProductId}",
                    imageId, productId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Обновить порядок изображений
        /// </summary>
        [HttpPut("{id}/images/order")]
        public async Task<IActionResult> UpdateImagesOrder(
            Guid id,
            [FromBody] UpdateImagesOrderDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                await _attachmentService.UpdateProductImagesOrderAsync(
                    id,
                    request.ImageOrders,
                    userId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении порядка изображений продукта {ProductId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        // ДОБАВИТЬ ЭТОТ МЕТОД:
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }


    public class UploadProductImageDto
    {
        public IFormFile File { get; set; } = null!;
        public bool IsMain { get; set; }
        public string AltText { get; set; } = string.Empty;
    }

    public class UpdateImagesOrderDto
    {
        public Dictionary<Guid, int> ImageOrders { get; set; } = new();
    }

    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool IsActive { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<AttachmentDto> Images { get; set; } = new();
        public AttachmentDto? MainImage => Images.FirstOrDefault(img => img.IsMain);
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid? CategoryId { get; set; }
        public int StockQuantity { get; set; }
    }
}
