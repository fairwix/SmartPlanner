// SmartPlanner.Application.Tests/Dtos/Files/ThumbnailTests.cs
using SmartPlanner.Application.Dtos.Files;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class ThumbnailTests
    {
        [Theory]
        [InlineData(ThumbnailSize.Small, 0)]
        [InlineData(ThumbnailSize.Medium, 1)]
        [InlineData(ThumbnailSize.Large, 2)]
        public void ThumbnailSize_EnumValues_AreCorrect(ThumbnailSize size, int expectedValue)
        {
            // Act & Assert
            Assert.Equal(expectedValue, (int)size);
        }

        [Fact]
        public void ThumbnailRequestDto_Properties_SetCorrectly()
        {
            // Act
            var dto = new ThumbnailRequestDto
            {
                Size = ThumbnailSize.Medium,
                Width = 600,
                Height = 400,
                Crop = true
            };

            // Assert
            Assert.Equal(ThumbnailSize.Medium, dto.Size);
            Assert.Equal(600, dto.Width);
            Assert.Equal(400, dto.Height);
            Assert.True(dto.Crop);
        }

        [Fact]
        public void ThumbnailRequestDto_DefaultValues_AreCorrect()
        {
            // Act
            var dto = new ThumbnailRequestDto();

            // Assert
            Assert.Equal(ThumbnailSize.Small, dto.Size);
            Assert.Null(dto.Width);
            Assert.Null(dto.Height);
            Assert.False(dto.Crop);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(300, 300, true)]
        [InlineData(800, null, false)]
        [InlineData(null, 600, false)]
        public void ThumbnailRequestDto_NullableProperties_WorkCorrectly(int? width, int? height, bool crop)
        {
            // Act
            var dto = new ThumbnailRequestDto
            {
                Width = width,
                Height = height,
                Crop = crop
            };

            // Assert
            Assert.Equal(width, dto.Width);
            Assert.Equal(height, dto.Height);
            Assert.Equal(crop, dto.Crop);
        }

        [Fact]
        public void ThumbnailRequestDto_ToString_ReturnsUsefulInfo()
        {
            // Arrange
            var dto = new ThumbnailRequestDto
            {
                Size = ThumbnailSize.Large,
                Width = 1920,
                Height = 1080,
                Crop = false
            };

            // Act
            var result = dto.ToString();

            // Assert
            Assert.Contains("Large", result);
            Assert.Contains("1920", result);
            Assert.Contains("1080", result);
        }
    }
}
