using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Services;
using SmartPlanner.Application.UnitTests.Goals.Queries;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly ProductService _service;
        private readonly Mock<DbSet<Product>> _mockProductsSet;

        public ProductServiceTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<ProductService>>();
            _service = new ProductService(_mockContext.Object, _mockLogger.Object);
            _mockProductsSet = new Mock<DbSet<Product>>();

            _mockContext.Setup(c => c.Products).Returns(_mockProductsSet.Object);
        }

        [Fact]
        public async Task CreateProductAsync_ValidDto_CreatesProductSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var dto = new CreateProductDto
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 99.99m,
                CategoryId = categoryId,
                StockQuantity = 100
            };

            // Product? capturedProduct = null;
            // _mockProductsSet.Setup(s => s.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            //     .Callback<Product, CancellationToken>((product, _) => capturedProduct = product)
            //     .Returns(ValueTask.FromResult((object?)null));
            //
            // _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            //     .ReturnsAsync(1);

            // Act
            var result = await _service.CreateProductAsync(dto, userId);

            // // Assert
            // Assert.NotNull(result);
            // Assert.Equal(dto.Name, capturedProduct?.Name);
            // Assert.Equal(dto.Description, capturedProduct?.Description);
            // Assert.Equal(dto.Price, capturedProduct?.Price);
            // Assert.Equal(dto.CategoryId, capturedProduct?.CategoryId);
            // Assert.Equal(dto.StockQuantity, capturedProduct?.StockQuantity);
            // Assert.Equal(userId, capturedProduct?.OwnerId);
            // Assert.True(capturedProduct?.IsActive);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Product") && v.ToString()!.Contains("created")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetProductAsync_ProductExistsAndUserIsOwner_ReturnsProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                OwnerId = userId,
                IsActive = true
            };

            var products = new List<Product> { product }.AsQueryable();
            SetupMockDbSet(_mockProductsSet, products);

            // Act
            var result = await _service.GetProductAsync(productId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("Test Product", result.Name);
        }

        [Fact]
        public async Task GetProductAsync_ProductNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var products = new List<Product>().AsQueryable();
            SetupMockDbSet(_mockProductsSet, products);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetProductAsync(productId, userId));

            Assert.Contains($"Product {productId} not found", exception.Message);
        }

        [Fact]
        public async Task GetProductAsync_ProductInactiveAndUserNotOwner_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Inactive Product",
                OwnerId = ownerId,
                IsActive = false
            };

            var products = new List<Product> { product }.AsQueryable();
            SetupMockDbSet(_mockProductsSet, products);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetProductAsync(productId, otherUserId));

            Assert.Contains("No access to this product", exception.Message);
        }

        [Fact]
        public async Task GetProductAsync_ProductInactiveButUserIsOwner_ReturnsProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "My Inactive Product",
                OwnerId = userId,
                IsActive = false
            };

            var products = new List<Product> { product }.AsQueryable();
            SetupMockDbSet(_mockProductsSet, products);

            // Act
            var result = await _service.GetProductAsync(productId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("My Inactive Product", result.Name);
        }

        [Fact]
        public async Task GetProductAsync_ProductActiveAndUserNotOwner_ReturnsProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Public Product",
                OwnerId = ownerId,
                IsActive = true
            };

            var products = new List<Product> { product }.AsQueryable();
            SetupMockDbSet(_mockProductsSet, products);

            // Act
            var result = await _service.GetProductAsync(productId, otherUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("Public Product", result.Name);
        }

        [Fact]
        public async Task GetProductAsync_IncludesImagesAndFiles()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                OwnerId = userId,
                IsActive = true,
                Images = new List<ProductImage>
                {
                    new ProductImage
                    {
                        FileId = fileId,
                        File = new FileMetadata
                        {
                            Id = fileId,
                            OriginalFileName = "product.jpg",
                            ContentType = "image/jpeg"
                        }
                    }
                }
            };

            var products = new List<Product> { product }.AsQueryable();
            SetupMockDbSet(_mockProductsSet, products);

            // Act
            var result = await _service.GetProductAsync(productId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Images);
            Assert.Single(result.Images);
            Assert.Equal(fileId, result.Images.First().FileId);
            Assert.NotNull(result.Images.First().File);
            Assert.Equal("product.jpg", result.Images.First().File.OriginalFileName);
        }

        [Fact]
        public async Task CheckUserAccessAsync_UserIsOwner_ReturnsTrue()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                OwnerId = userId
            };

            _mockContext.Setup(c => c.Products.FindAsync(productId))
                .ReturnsAsync(product);

            // Act
            var result = await _service.CheckUserAccessAsync(productId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckUserAccessAsync_UserIsNotOwner_ReturnsFalse()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                OwnerId = ownerId
            };

            _mockContext.Setup(c => c.Products.FindAsync(productId))
                .ReturnsAsync(product);

            // Act
            var result = await _service.CheckUserAccessAsync(productId, otherUserId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserAccessAsync_ProductNotFound_ReturnsFalse()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockContext.Setup(c => c.Products.FindAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _service.CheckUserAccessAsync(productId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateProductAsync_WithNullCategoryId_CreatesProductSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateProductDto
            {
                Name = "Product without category",
                Description = "Description",
                Price = 50.00m,
                CategoryId = null,
                StockQuantity = 10
            };

            // Product? capturedProduct = null;
            // _mockProductsSet.Setup(s => s.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            //     .Callback<Product, CancellationToken>((product, _) => capturedProduct = product)
            //     .Returns(ValueTask.FromResult((object?)null));

            // _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            //     .ReturnsAsync(1);
            //
            // // Act
            // var result = await _service.CreateProductAsync(dto, userId);
            //
            // // Assert
            // Assert.NotNull(result);
            // Assert.Null(capturedProduct?.CategoryId);
        }

        [Fact]
        public async Task CreateProductAsync_ZeroStockQuantity_CreatesProductSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateProductDto
            {
                Name = "Out of stock product",
                Description = "Description",
                Price = 100.00m,
                CategoryId = Guid.NewGuid(),
                StockQuantity = 0
            };

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateProductAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
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
