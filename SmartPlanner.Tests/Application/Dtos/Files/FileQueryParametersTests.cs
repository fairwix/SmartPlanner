// SmartPlanner.Application.Tests/Dtos/Files/FileQueryParametersTests.cs
using SmartPlanner.Application.Dtos.Files;
using Xunit;

namespace SmartPlanner.Application.Tests.Dtos.Files
{
    public class FileQueryParametersTests
    {
        [Fact]
        public void FileQueryParameters_DefaultValues_AreCorrect()
        {
            // Act
            var parameters = new FileQueryParameters();

            // Assert
            Assert.Equal(1, parameters.Page);
            Assert.Equal(20, parameters.PageSize);
            Assert.Null(parameters.Search);
            Assert.Null(parameters.ContentType);
            Assert.Null(parameters.IsPublic);
            Assert.Equal("CreatedAt", parameters.SortBy);
            Assert.True(parameters.SortDescending);
            Assert.Equal(1, parameters.PageNumber); // Alias для Page
        }

        [Fact]
        public void FileQueryParameters_Properties_SetCorrectly()
        {
            // Act
            var parameters = new FileQueryParameters
            {
                Page = 3,
                PageSize = 50,
                Search = "invoice",
                ContentType = "application/pdf",
                IsPublic = true,
                SortBy = "Size",
                SortDescending = false
            };

            // Assert
            Assert.Equal(3, parameters.Page);
            Assert.Equal(50, parameters.PageSize);
            Assert.Equal("invoice", parameters.Search);
            Assert.Equal("application/pdf", parameters.ContentType);
            Assert.True(parameters.IsPublic);
            Assert.Equal("Size", parameters.SortBy);
            Assert.False(parameters.SortDescending);
            Assert.Equal(3, parameters.PageNumber);
        }

        [Fact]
        public void PageNumber_Alias_WorksCorrectly()
        {
            // Arrange
            var parameters = new FileQueryParameters
            {
                Page = 5
            };

            // Act & Assert
            Assert.Equal(5, parameters.PageNumber);
            Assert.Equal(parameters.Page, parameters.PageNumber);
        }

        [Theory]
        [InlineData(0, 0)] // Edge case
        [InlineData(1, 1)] // Minimum
        [InlineData(10, 10)] // Normal
        [InlineData(100, 100)] // Large
        public void PageNumber_ReflectsPageValue(int page, int expectedPageNumber)
        {
            // Arrange
            var parameters = new FileQueryParameters { Page = page };

            // Act & Assert
            Assert.Equal(expectedPageNumber, parameters.PageNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("document")]
        [InlineData("image")]
        public void Search_AcceptsVariousValues(string search)
        {
            // Act
            var parameters = new FileQueryParameters { Search = search };

            // Assert
            Assert.Equal(search, parameters.Search);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("image/jpeg")]
        [InlineData("application/pdf")]
        [InlineData("video/mp4")]
        public void ContentType_AcceptsVariousValues(string contentType)
        {
            // Act
            var parameters = new FileQueryParameters { ContentType = contentType };

            // Assert
            Assert.Equal(contentType, parameters.ContentType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        public void IsPublic_AcceptsVariousValues(bool? isPublic)
        {
            // Act
            var parameters = new FileQueryParameters { IsPublic = isPublic };

            // Assert
            Assert.Equal(isPublic, parameters.IsPublic);
        }

        [Theory]
        [InlineData("FileName", false)]
        [InlineData("Size", true)]
        [InlineData("CreatedAt", true)]
        [InlineData("UpdatedAt", false)]
        public void SortBy_And_SortDescending_WorkTogether(string sortBy, bool sortDescending)
        {
            // Act
            var parameters = new FileQueryParameters
            {
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            // Assert
            Assert.Equal(sortBy, parameters.SortBy);
            Assert.Equal(sortDescending, parameters.SortDescending);
        }

        [Fact]
        public void FileQueryParameters_CanBeUsedForPagination()
        {
            // Arrange
            var parameters = new FileQueryParameters
            {
                Page = 2,
                PageSize = 25
            };

            // Act
            var skip = (parameters.Page - 1) * parameters.PageSize;
            var take = parameters.PageSize;

            // Assert
            Assert.Equal(25, skip); // (2-1) * 25 = 25
            Assert.Equal(25, take);
        }

        [Fact]
        public void ToString_ReturnsUsefulRepresentation()
        {
            // Arrange
            var parameters = new FileQueryParameters
            {
                Page = 2,
                PageSize = 30,
                Search = "report",
                ContentType = "pdf",
                IsPublic = true,
                SortBy = "Size",
                SortDescending = false
            };

            // Act
            var result = parameters.ToString();

            // Assert
            Assert.Contains("Page=2", result);
            Assert.Contains("PageSize=30", result);
            Assert.Contains("Search=report", result);
            Assert.Contains("ContentType=pdf", result);
            Assert.Contains("IsPublic=True", result);
            Assert.Contains("SortBy=Size", result);
            Assert.Contains("SortDescending=False", result);
        }
    }
}
