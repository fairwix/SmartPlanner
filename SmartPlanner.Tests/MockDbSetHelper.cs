using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Moq;

namespace SmartPlanner.Tests
{
    public static class MockDbSetHelper
    {
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();
            
            // Setup synchronous query methods
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
                .Returns(new AsyncQueryProvider<T>(queryable.Provider));
                
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression)
                .Returns(queryable.Expression);
                
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType)
                .Returns(queryable.ElementType);
                
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator())
                .Returns(() => queryable.GetEnumerator());

            // Setup async methods
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            // Setup Include and other EF Core methods
            mockSet.Setup(m => m.Include(It.IsAny<string>()))
                .Returns(mockSet.Object);
                
            mockSet.Setup(m => m.AsNoTracking())
                .Returns(mockSet.Object);

            return mockSet;
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    internal class AsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal AsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new AsyncEnumerable<T>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new AsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new AsyncEnumerable<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult);
            var executeMethod = _inner.GetType()
                .GetMethods()
                .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(resultType.GenericTypeArguments[0]);

            return (TResult)executeMethod.Invoke(_inner, new object[] { expression });
        }
    }

    internal class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public AsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public AsyncEnumerable(Expression expression)
            : base(expression)
        { }

        IQueryProvider IQueryable.Provider => new AsyncQueryProvider<T>(this);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
    }
}
