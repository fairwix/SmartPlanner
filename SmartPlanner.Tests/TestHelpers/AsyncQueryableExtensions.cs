using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace SmartPlanner.Tests.TestHelpers
{
    public static class AsyncQueryableExtensions
    {
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();

            var mockSet = new Mock<DbSet<T>>();

            // Синхронные IQueryable методы
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            // Асинхронные методы EF Core
            mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns((T entity, CancellationToken ct) => Task.FromResult(entity));

            mockSet.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] keyValues) =>
                {
                    // Для простоты ищем по первому Guid, если он есть
                    if (typeof(T).GetProperty("Id") != null && keyValues.Length > 0)
                    {
                        var id = keyValues[0] as Guid?;
                        if (id.HasValue)
                        {
                            return data.FirstOrDefault(x =>
                                x.GetType().GetProperty("Id")?.GetValue(x)?.Equals(id.Value) == true);
                        }
                    }
                    return null;
                });

            return mockSet;
        }
    }
}
