using FluentAssertions;
using SmartPlanner.Application.Common.Dtos;
using Xunit;

namespace SmartPlanner.Application.Tests.Common.Dtos
{
    public class BulkOperationsTests
    {
        [Fact]
        public void BulkOperationResult_ShouldInitializeCorrectly()
        {
            // Arrange
            var result = new BulkOperationResult<string>();

            // Assert
            result.Items.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.SuccessfulCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            result.AllSucceeded.Should().BeTrue(); // 0/0 считается успехом
            result.SuccessRate.Should().Be(0);
        }

        [Fact]
        public void BulkOperationResult_WithItems_ShouldCalculateCorrectly()
        {
            // Arrange
            var result = new BulkOperationResult<string>
            {
                Items = new List<BulkOperationItem<string>>
                {
                    new() { Success = true },
                    new() { Success = true },
                    new() { Success = false },
                    new() { Success = true }
                },
                TotalCount = 4
            };

            // Act
            result.SuccessfulCount = result.Items.Count(i => i.Success);
            result.FailedCount = result.Items.Count(i => !i.Success);

            // Assert
            result.SuccessfulCount.Should().Be(3);
            result.FailedCount.Should().Be(1);
            result.AllSucceeded.Should().BeFalse();
            result.SuccessRate.Should().Be(75.0); // 3/4 * 100 = 75%
        }

        [Theory]
        [InlineData(0, 0, 0, 0, true, 0)]
        [InlineData(4, 4, 0, 0, true, 100)]
        [InlineData(4, 3, 1, 0, false, 75)]
        [InlineData(4, 0, 4, 0, false, 0)]
        public void BulkOperationResult_Properties_ShouldCalculateCorrectly(
            int total, int successful, int failed, int itemsCount, bool allSucceeded, double successRate)
        {
            // Arrange & Act
            var result = new BulkOperationResult<string>
            {
                TotalCount = total,
                SuccessfulCount = successful,
                FailedCount = failed
            };

            // Assert
            result.TotalCount.Should().Be(total);
            result.SuccessfulCount.Should().Be(successful);
            result.FailedCount.Should().Be(failed);
            result.AllSucceeded.Should().Be(allSucceeded);
            result.SuccessRate.Should().Be(successRate);
        }

        [Fact]
        public void BulkOperationItem_ShouldInitializeCorrectly()
        {
            // Arrange
            var item = new BulkOperationItem<string>
            {
                Data = "Test Data",
                Success = true,
                Message = "Operation successful",
                Error = null,
                ItemId = Guid.NewGuid()
            };

            // Assert
            item.Data.Should().Be("Test Data");
            item.Success.Should().BeTrue();
            item.Message.Should().Be("Operation successful");
            item.Error.Should().BeNull();
            item.ItemId.Should().NotBeNull();
        }

        [Fact]
        public void BulkOperationItem_WithError_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var error = "Something went wrong";
            var item = new BulkOperationItem<string>
            {
                Data = null,
                Success = false,
                Message = "Operation failed",
                Error = error,
                ItemId = null
            };

            // Assert
            item.Data.Should().BeNull();
            item.Success.Should().BeFalse();
            item.Message.Should().Be("Operation failed");
            item.Error.Should().Be(error);
            item.ItemId.Should().BeNull();
        }

        [Fact]
        public void BulkDeleteResult_ShouldInitializeCorrectly()
        {
            // Arrange
            var result = new BulkDeleteResult();

            // Assert
            result.Items.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.SuccessfulCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
        }

        [Fact]
        public void BulkDeleteResult_WithItems_ShouldCalculateCorrectly()
        {
            // Arrange
            var result = new BulkDeleteResult
            {
                Items = new List<BulkDeleteItem>
                {
                    new() { Id = Guid.NewGuid(), Success = true },
                    new() { Id = Guid.NewGuid(), Success = false },
                    new() { Id = Guid.NewGuid(), Success = true }
                },
                TotalCount = 3
            };

            // Act
            result.SuccessfulCount = result.Items.Count(i => i.Success);
            result.FailedCount = result.Items.Count(i => !i.Success);

            // Assert
            result.SuccessfulCount.Should().Be(2);
            result.FailedCount.Should().Be(1);
        }

        [Fact]
        public void BulkDeleteItem_ShouldInitializeCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var item = new BulkDeleteItem
            {
                Id = id,
                Success = true,
                Message = "Deleted successfully",
                Error = null
            };

            // Assert
            item.Id.Should().Be(id);
            item.Success.Should().BeTrue();
            item.Message.Should().Be("Deleted successfully");
            item.Error.Should().BeNull();
        }

        [Fact]
        public void BulkDeleteItem_WithError_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var error = "Item not found";
            var item = new BulkDeleteItem
            {
                Id = id,
                Success = false,
                Message = "Failed to delete",
                Error = error
            };

            // Assert
            item.Id.Should().Be(id);
            item.Success.Should().BeFalse();
            item.Message.Should().Be("Failed to delete");
            item.Error.Should().Be(error);
        }

        [Fact]
        public void BulkOperationResult_CalculateProperties_ShouldWorkAfterAddingItems()
        {
            // Arrange
            var result = new BulkOperationResult<int>
            {
                Items = new List<BulkOperationItem<int>>
                {
                    new() { Data = 1, Success = true },
                    new() { Data = 2, Success = false },
                    new() { Data = 3, Success = true },
                    new() { Data = 4, Success = true }
                }
            };

            // Act
            result.TotalCount = result.Items.Count;
            result.SuccessfulCount = result.Items.Count(i => i.Success);
            result.FailedCount = result.Items.Count(i => !i.Success);

            // Assert
            result.TotalCount.Should().Be(4);
            result.SuccessfulCount.Should().Be(3);
            result.FailedCount.Should().Be(1);
            result.SuccessRate.Should().Be(75.0);
            result.AllSucceeded.Should().BeFalse();
        }

        [Fact]
        public void BulkOperations_Collections_ShouldBeMutable()
        {
            // Arrange
            var bulkResult = new BulkOperationResult<string>();
            var deleteResult = new BulkDeleteResult();

            // Act
            bulkResult.Items.Add(new BulkOperationItem<string> { Data = "test" });
            deleteResult.Items.Add(new BulkDeleteItem { Id = Guid.NewGuid() });

            // Assert
            bulkResult.Items.Should().HaveCount(1);
            deleteResult.Items.Should().HaveCount(1);
        }
    }
}
