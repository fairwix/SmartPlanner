// SmartPlanner.Tests/API/Filters/RateLimitAttributeTests.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using SmartPlanner.API.Filters;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace SmartPlanner.Tests.API.Filters
{
    public class RateLimitAttributeTests
    {
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly RateLimitAttribute _attribute;
        private readonly ActionExecutingContext _context;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockRequest;
        private readonly Mock<HttpResponse> _mockResponse;

        public RateLimitAttributeTests()
        {
            _mockCache = new Mock<IMemoryCache>();
            _attribute = new RateLimitAttribute("test", limit: 5, seconds: 60);

            _mockHttpContext = new Mock<HttpContext>();
            _mockRequest = new Mock<HttpRequest>();
            _mockResponse = new Mock<HttpResponse>();

            _mockHttpContext.SetupGet(c => c.Request).Returns(_mockRequest.Object);
            _mockHttpContext.SetupGet(c => c.Response).Returns(_mockResponse.Object);
            _mockHttpContext.SetupGet(c => c.Connection).Returns(new Mock<ConnectionInfo>().Object);

            _mockRequest.SetupGet(r => r.Headers).Returns(new HeaderDictionary());
            _mockRequest.SetupGet(r => r.HttpContext).Returns(_mockHttpContext.Object);

            var actionDescriptor = new ActionDescriptor();
            var routeData = new RouteData();
            var actionContext = new ActionContext(
                _mockHttpContext.Object,
                routeData,
                actionDescriptor,
                new ModelStateDictionary());

            _context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new Mock<Controller>().Object
            );

            _context.HttpContext.RequestServices = new Mock<IServiceProvider>().Object;
        }

        [Fact]
        public void OnActionExecuting_FirstRequest_AllowsExecution()
        {
            // Arrange
            _mockHttpContext.SetupGet(c => c.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.1"));
            _mockHttpContext.SetupGet(c => c.RequestServices).Returns(CreateServiceProvider());

            var cacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<int>.IsAny)).Returns(false);

            // Act
            _attribute.OnActionExecuting(_context);

            // Assert
            Assert.Null(_context.Result); // No result means execution continues
            _mockCache.Verify(c => c.Set(It.IsAny<object>(), It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void OnActionExecuting_ExceedsLimit_Returns429Result()
        {
            // Arrange
            _mockHttpContext.SetupGet(c => c.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.1"));
            _mockHttpContext.SetupGet(c => c.RequestServices).Returns(CreateServiceProvider());

            int requestCount = 5;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out requestCount)).Returns(true);

            // Act
            _attribute.OnActionExecuting(_context);

            // Assert
            Assert.NotNull(_context.Result);
            var objectResult = Assert.IsType<ObjectResult>(_context.Result);
            Assert.Equal(429, objectResult.StatusCode);
            var response = Assert.IsType<Dictionary<string, object>>(objectResult.Value);
            Assert.Equal(429, response["StatusCode"]);
            Assert.Contains("Rate limit exceeded", response["Message"].ToString());
        }

        [Fact]
        public void OnActionExecuting_NoMemoryCache_ThrowsException()
        {
            // Arrange
            _mockHttpContext.SetupGet(c => c.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.1"));
            _mockHttpContext.SetupGet(c => c.RequestServices).Returns(new Mock<IServiceProvider>().Object);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _attribute.OnActionExecuting(_context));
            Assert.Equal("IMemoryCache is not registered in the service container", exception.Message);
        }

        private IServiceProvider CreateServiceProvider()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IMemoryCache)))
                .Returns(_mockCache.Object);
            return serviceProviderMock.Object;
        }
    }
}
