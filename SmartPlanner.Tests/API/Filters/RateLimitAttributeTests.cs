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
        private readonly MemoryCache _mockCache;
        private readonly RateLimitAttribute _attribute;
        private readonly ActionExecutingContext _context;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockRequest;
        private readonly Mock<HttpResponse> _mockResponse;

        public RateLimitAttributeTests()
        {
            _mockCache = new MemoryCache(new MemoryCacheOptions());
            _attribute = new RateLimitAttribute("test", limit: 5, seconds: 60);

            _mockHttpContext = new Mock<HttpContext>();
            _mockRequest = new Mock<HttpRequest>();
            _mockResponse = new Mock<HttpResponse>();

            _mockHttpContext.SetupGet(c => c.Request).Returns(_mockRequest.Object);
            _mockHttpContext.SetupGet(c => c.Response).Returns(_mockResponse.Object);
            _mockHttpContext.SetupGet(c => c.Connection).Returns(new Mock<ConnectionInfo>().Object);

            _mockRequest.SetupGet(r => r.Headers).Returns(new HeaderDictionary());
            _mockResponse.SetupGet(r => r.Headers).Returns(new HeaderDictionary());
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

            // Act
            _attribute.OnActionExecuting(_context);

            // Assert
            Assert.Null(_context.Result); // No result means execution continues
            Assert.Equal(1, _mockCache.Get<int>("test:192.168.1.1"));
        }

        [Fact]
        public void OnActionExecuting_ExceedsLimit_Returns429Result()
        {
            // Arrange
            _mockHttpContext.SetupGet(c => c.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("192.168.1.1"));
            _mockHttpContext.SetupGet(c => c.RequestServices).Returns(CreateServiceProvider());

            _mockCache.Set("test:192.168.1.1", 5);

            // Act
            _attribute.OnActionExecuting(_context);

            // Assert
            Assert.NotNull(_context.Result);
            var objectResult = Assert.IsType<ObjectResult>(_context.Result);
            Assert.Equal(429, objectResult.StatusCode);
            using var response = JsonDocument.Parse(JsonSerializer.Serialize(objectResult.Value));
            Assert.Equal(429, response.RootElement.GetProperty("StatusCode").GetInt32());
            Assert.Contains("Rate limit exceeded", response.RootElement.GetProperty("Message").GetString());
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
            Assert.Equal("IMemoryCache is not registered", exception.Message);
        }

        private IServiceProvider CreateServiceProvider()
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IMemoryCache)))
                .Returns(_mockCache);
            return serviceProviderMock.Object;
        }
    }
}
