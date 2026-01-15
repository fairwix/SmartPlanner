using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Services
{
    public interface IProductService
    {
        Task<Product> CreateProductAsync(CreateProductDto dto, Guid userId);
        Task<Product> GetProductAsync(Guid productId, Guid userId);
        Task<bool> CheckUserAccessAsync(Guid productId, Guid userId);
    }

    public class ProductService : IProductService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Product> CreateProductAsync(CreateProductDto dto, Guid userId)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                OwnerId = userId,
                CategoryId = dto.CategoryId,
                IsActive = true,
                StockQuantity = dto.StockQuantity
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product {ProductId} created by user {UserId}", product.Id, userId);
            return product;
        }

        public async Task<Product> GetProductAsync(Guid productId, Guid userId)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .ThenInclude(i => i.File)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new KeyNotFoundException($"Product {productId} not found");

            if (!product.IsActive && product.OwnerId != userId)
                throw new UnauthorizedAccessException("No access to this product");

            return product;
        }

        public async Task<bool> CheckUserAccessAsync(Guid productId, Guid userId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            return product.OwnerId == userId;
        }
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
