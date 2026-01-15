// SmartPlanner.Tests/Infrastructure/Common/PaginatedListTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using SmartPlanner.Infrastructure;

using SmartPlanner.Infrastructure.Data;
using Xunit;
using FluentAssertions;
using SmartPlanner.Application.Common;
using SmartPlanner.Infrastructure.Common;

namespace SmartPlanner.Tests.Infrastructure.Common
{
    public class PaginatedListTests
    {
        [Fact]
        public async Task CreateAsync_ValidSource_ReturnsPaginatedList()
        {
            // Arrange
            var source = Enumerable.Range(1, 100)
                .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
                .ToList();

            int pageNumber = 2;
            int pageSize = 10;

            var mockContext = new Mock<AppDbContext>();
            var mockSet = MockDbSetHelper.CreateMockDbSet(source);
            mockContext.Setup(c => c.Set<TestEntity>()).Returns(mockSet.Object);

            // Act
            var result = await PaginatedList<TestEntity>.CreateAsync(mockContext.Object.Set<TestEntity>(), pageNumber, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(pageSize);
            result.TotalCount.Should().Be(100);
            result.PageNumber.Should().Be(pageNumber);
            result.TotalPages.Should().Be(10);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();

            // Verify items are correct for page 2
            result.Items.First().Id.Should().Be(11);
            result.Items.Last().Id.Should().Be(20);
        }

        [Fact]
        public async Task CreateAsync_LastPage_ReturnsPaginatedList()
        {
            // Arrange
            var source = Enumerable.Range(1, 100)
                .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
                .ToList();

            int pageNumber = 10;
            int pageSize = 10;

            var mockContext = new Mock<AppDbContext>();
            var mockSet = MockDbSetHelper.CreateMockDbSet(source);
            mockContext.Setup(c => c.Set<TestEntity>()).Returns(mockSet.Object);

            // Act
            var result = await PaginatedList<TestEntity>.CreateAsync(mockContext.Object.Set<TestEntity>(), pageNumber, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(10);
            result.PageNumber.Should().Be(pageNumber);
            result.TotalPages.Should().Be(10);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_EmptySource_ReturnsEmptyPaginatedList()
        {
            // Arrange
            var source = new List<TestEntity>();
            int pageNumber = 1;
            int pageSize = 10;

            var mockContext = new Mock<AppDbContext>();
            var mockSet = MockDbSetHelper.CreateMockDbSet(source);
            mockContext.Setup(c => c.Set<TestEntity>()).Returns(mockSet.Object);

            // Act
            var result = await PaginatedList<TestEntity>.CreateAsync(mockContext.Object.Set<TestEntity>(), pageNumber, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_PageSizeLargerThanSource_ReturnsAllItems()
        {
            // Arrange
            var source = Enumerable.Range(1, 5)
                .Select(i => new TestEntity { Id = i, Name = $"Item {i}" })
                .ToList();

            int pageNumber = 1;
            int pageSize = 10;

            var mockContext = new Mock<AppDbContext>();
            var mockSet = MockDbSetHelper.CreateMockDbSet(source);
            mockContext.Setup(c => c.Set<TestEntity>()).Returns(mockSet.Object);

            // Act
            var result = await PaginatedList<TestEntity>.CreateAsync(mockContext.Object.Set<TestEntity>(), pageNumber, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(5);
            result.TotalPages.Should().Be(1);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        }

        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
