using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace SmartPlanner.Tests.TestHelpers;

public static class AsyncQueryableExtensions
{
    public static IQueryProvider CreateAsyncQueryProvider<T>(IQueryable<T> source) =>
        new TestAsyncQueryProvider<T>(source.Provider);

    public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        ConfigureMockDbSet(mockSet, data.AsQueryable());
        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Returns((T entity, CancellationToken _) => ValueTask.FromResult<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>>(null!));
        mockSet.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<T>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mockSet;
    }

    public static void ConfigureMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> source) where T : class
    {
        var asyncQueryable = new TestAsyncEnumerable<T>(source.Expression, source.Provider);
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) => asyncQueryable.GetAsyncEnumerator(token));
        var queryable = (IQueryable<T>)asyncQueryable;
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        private readonly IQueryProvider _innerProvider;
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) => _innerProvider = enumerable.AsQueryable().Provider;
        public TestAsyncEnumerable(Expression expression, IQueryProvider innerProvider) : base(expression) => _innerProvider = innerProvider;

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(_innerProvider);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryable<TEntity> queryable) => _inner = ((IQueryable)queryable).Provider;
        public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        public IQueryable CreateQuery(Expression expression) =>
            (IQueryable)Activator.CreateInstance(typeof(TestAsyncEnumerable<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), expression, _inner)!;

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression, _inner);
        public object? Execute(Expression expression) => _inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .Single(m => m.Name == nameof(Execute) && m.IsGenericMethod)
                .MakeGenericMethod(resultType)
                .Invoke(_inner, new object[] { expression });
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, new[] { executionResult })!;
        }
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    }
}
