using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace SmartPlanner.Tests.Helpers
{
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

public static class MockDbSetHelper
    {
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
            => SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateMockDbSet(data);
    }
}
