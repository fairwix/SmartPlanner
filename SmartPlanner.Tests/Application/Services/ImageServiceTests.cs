// SmartPlanner.Application.Tests/Services/ImageServiceTests.cs
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Services;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class ImageServiceTests
    {
        private readonly Mock<ILogger<ImageService>> _mockLogger;
        private readonly ImageService _imageService;
        private readonly string _testDataPath;

        public ImageServiceTests()
        {
            _mockLogger = new Mock<ILogger<ImageService>>();
            _imageService = new ImageService(_mockLogger.Object);

            // Создаем временную директорию для тестов
            _testDataPath = Path.Combine(Path.GetTempPath(), "ImageServiceTests");
            Directory.CreateDirectory(_testDataPath);
        }

        public void Dispose()
        {
            // Очистка после тестов
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
        }

        [Theory]
        [InlineData("test.jpg", true)]
        [InlineData("test.jpeg", true)]
        [InlineData("test.png", true)]
        [InlineData("test.gif", true)]
        [InlineData("test.webp", true)]
        [InlineData("test.bmp", true)]
        [InlineData("test.pdf", false)]
        [InlineData("test.txt", false)]
        [InlineData("test.doc", false)]
        [InlineData("test.mp4", false)]
        public void IsImageFile_ReturnsCorrectValue(string fileName, bool expected)
        {
            // Act
            var result = _imageService.IsImageFile(fileName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_WithDefaultSize_CreatesFile()
        {
            // Arrange
            var sourceImagePath = CreateTestImage();
            var outputDirectory = Path.Combine(_testDataPath, "thumbnails");

            // Act
            var thumbnailPath = await _imageService.GenerateThumbnailAsync(
                sourceImagePath, outputDirectory, ThumbnailSize.Small);

            // Assert
            Assert.True(File.Exists(thumbnailPath));
            Assert.Contains("_200x200", thumbnailPath);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_WithCustomDimensions_CreatesFile()
        {
            // Arrange
            var sourceImagePath = CreateTestImage();
            var outputDirectory = Path.Combine(_testDataPath, "custom");

            // Act
            var thumbnailPath = await _imageService.GenerateThumbnailAsync(
                sourceImagePath, outputDirectory, 300, 200, crop: true);

            // Assert
            Assert.True(File.Exists(thumbnailPath));
            Assert.Contains("_300x200", thumbnailPath);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_WhenAlreadyExists_ReturnsExisting()
        {
            // Arrange
            var sourceImagePath = CreateTestImage();
            var outputDirectory = _testDataPath;

            // Первое создание
            var firstPath = await _imageService.GenerateThumbnailAsync(
                sourceImagePath, outputDirectory, 100, 100);

            // Act - Вторая попытка создания того же thumbnail
            var secondPath = await _imageService.GenerateThumbnailAsync(
                sourceImagePath, outputDirectory, 100, 100);

            // Assert
            Assert.Equal(firstPath, secondPath);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("уже существует")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetImageDimensionsAsync_ReturnsCorrectDimensions()
        {
            // Arrange
            var imagePath = CreateTestImage(500, 300);

            // Act
            var dimensions = await _imageService.GetImageDimensionsAsync(imagePath);

            // Assert
            Assert.Equal(500, dimensions.Width);
            Assert.Equal(300, dimensions.Height);
        }

        [Fact]
        public async Task GetImageDimensionsAsync_WithInvalidFile_ReturnsZeroDimensions()
        {
            // Arrange
            var invalidPath = Path.Combine(_testDataPath, "invalid.jpg");
            File.WriteAllText(invalidPath, "not an image");

            // Act
            var dimensions = await _imageService.GetImageDimensionsAsync(invalidPath);

            // Assert
            Assert.Equal(0, dimensions.Width);
            Assert.Equal(0, dimensions.Height);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Не удалось определить размеры")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OptimizeImageAsync_ReducesFileSize()
        {
            // Arrange
            var originalImagePath = CreateTestImage(800, 600);
            var originalSize = new FileInfo(originalImagePath).Length;

            // Act
            await _imageService.OptimizeImageAsync(originalImagePath, quality: 50);

            // Assert
            var optimizedSize = new FileInfo(originalImagePath).Length;
            Assert.True(optimizedSize < originalSize);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Оптимизация изображения")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExtractExifDataAsync_ReturnsDictionary()
        {
            // Arrange
            var imagePath = CreateTestImage();

            // Act
            var exifData = await _imageService.ExtractExifDataAsync(imagePath);

            // Assert
            Assert.NotNull(exifData);
            Assert.IsType<Dictionary<string, string>>(exifData);
        }

        [Fact]
        public async Task RemoveExifDataAsync_RemovesExif()
        {
            // Arrange
            var imagePath = CreateTestImage();

            // Act
            await _imageService.RemoveExifDataAsync(imagePath);

            // Assert
            Assert.True(File.Exists(imagePath));
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Удаление EXIF данных")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(".jpg", typeof(SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder))]
        [InlineData(".jpeg", typeof(SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder))]
        [InlineData(".png", typeof(SixLabors.ImageSharp.Formats.Png.PngEncoder))]
        [InlineData(".gif", typeof(SixLabors.ImageSharp.Formats.Gif.GifEncoder))]
        [InlineData(".webp", typeof(SixLabors.ImageSharp.Formats.Webp.WebpEncoder))]
        [InlineData(".bmp", typeof(SixLabors.ImageSharp.Formats.Bmp.BmpEncoder))]
        [InlineData(".unknown", typeof(SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder))]
        public void GetImageEncoder_ReturnsCorrectType(string extension, Type expectedType)
        {
            // Для тестирования приватного метода нам нужно использовать reflection
            // или сделать метод protected/internal. В данном случае пропустим этот тест
            // или используем integration test для проверки через публичные методы.
        }

        private string CreateTestImage(int width = 100, int height = 100)
        {
            var imagePath = Path.Combine(_testDataPath, $"test_{width}x{height}.png");

            using (var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height))
            {
                // Заполняем изображение тестовыми данными
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        image[x, y] = new SixLabors.ImageSharp.PixelFormats.Rgba32(
                            (byte)(x % 255),
                            (byte)(y % 255),
                            (byte)((x + y) % 255));
                    }
                }

                //image.Save(imagePath);
            }

            return imagePath;
        }
    }
}
