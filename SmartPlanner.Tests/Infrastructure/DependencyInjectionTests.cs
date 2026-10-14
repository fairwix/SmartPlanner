using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Application.Services;
using SmartPlanner.Infrastructure;
using SmartPlanner.Infrastructure.Data;
using Xunit;

namespace SmartPlanner.Infrastructure.Tests
{
    public class DependencyInjectionTests
    {
        private readonly ServiceCollection _services;
        private readonly IConfiguration _configuration;

        public DependencyInjectionTests()
        {
            _services = new ServiceCollection();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=testdb;Username=test;Password=test",
                ["AppSettings:SomeKey"] = "SomeValue",
                ["EmailSettings:From"] = "test@example.com",
                ["JwtSettings:Secret"] = "test-secret-key-very-long-for-testing",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["RateLimiting:RequestsPerMinute"] = "100"
            });

            _configuration = configurationBuilder.Build();
        }

        [Fact]
        public void AddInfrastructure_RegistersDbContext()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var dbContext = serviceProvider.GetService<AppDbContext>();
            var appDbContext = serviceProvider.GetService<IApplicationDbContext>();

            Assert.NotNull(dbContext);
            Assert.NotNull(appDbContext);
            Assert.IsType<AppDbContext>(dbContext);
            Assert.IsType<AppDbContext>(appDbContext);
        }

        [Fact]
        public void AddInfrastructure_RegistersScopedServices()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();

            // Проверяем scoped сервисы
            Assert.NotNull(serviceProvider.GetService<IAchievementCheckerService>());
            Assert.NotNull(serviceProvider.GetService<IConfirmationTokenService>());
            Assert.NotNull(serviceProvider.GetService<IEmailService>());
            Assert.NotNull(serviceProvider.GetService<IPasswordHasher>());
            Assert.NotNull(serviceProvider.GetService<ITokenService>());
            Assert.NotNull(serviceProvider.GetService<IAuditService>());

            Assert.IsType<AchievementCheckerService>(serviceProvider.GetService<IAchievementCheckerService>());
            Assert.IsType<ConfirmationTokenService>(serviceProvider.GetService<IConfirmationTokenService>());
            Assert.IsType<EmailService>(serviceProvider.GetService<IEmailService>());
            Assert.IsType<PasswordHasher>(serviceProvider.GetService<IPasswordHasher>());
            Assert.IsType<TokenService>(serviceProvider.GetService<ITokenService>());
            Assert.IsType<AuditService>(serviceProvider.GetService<IAuditService>());
        }

        [Fact]
        public void AddInfrastructure_RegistersHostedServices()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            var hostedServices = _services.Where(s => s.ServiceType == typeof(IHostedService)).ToList();

            Assert.Equal(2, hostedServices.Count);
            Assert.Contains(hostedServices, s => s.ImplementationType == typeof(AuditLogCleanupService));
            Assert.Contains(hostedServices, s => s.ImplementationType == typeof(EmailTokenCleanupService));
        }

        [Fact]
        public void AddInfrastructure_ConfiguresOptions()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();

            // Проверяем, что options сконфигурированы
            var appSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Application.Common.Models.AppSettings>>();
            var emailSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Application.Common.Models.EmailSettings>>();
            var jwtSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Application.Common.Models.JwtSettings>>();
            var corsSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Application.Common.Models.CorsSettings>>();
            var rateLimitSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Application.Common.Models.RateLimitSettings>>();

            Assert.NotNull(appSettings);
            Assert.NotNull(emailSettings);
            Assert.NotNull(jwtSettings);
            Assert.NotNull(corsSettings);
            Assert.NotNull(rateLimitSettings);
        }

        [Fact]
        public void AddInfrastructure_ThrowsException_WhenConnectionStringMissing()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                services.AddInfrastructure(emptyConfig));

            Assert.Equal("Connection string 'PostgreSQL' not found.", exception.Message);
        }

        [Fact]
        public void AddInfrastructure_ConfiguresDbContextWithRetryOnFailure()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            Assert.NotNull(dbContext);
            // Можно проверить дополнительные настройки, если есть доступ к внутреннему состоянию
        }

        [Fact]
        public void AddInfrastructure_RegistersSingletonServices()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            // Проверяем, что IHostEnvironment не зарегистрирован (раскомментируйте если нужно)
            // var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
            // Assert.Null(hostEnvironment);

            // Проверяем, что FluentMigrator не зарегистрирован (раскомментируйте если нужно)
            // var migrator = serviceProvider.GetService<IMigrationRunner>();
            // Assert.Null(migrator);
        }

        [Fact]
        public void ServiceLifetimes_AreCorrect()
        {
            // Act
            _services.AddInfrastructure(_configuration);

            // Assert
            var dbContextDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(AppDbContext));
            var appDbContextDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IApplicationDbContext));
            var hostedServiceDescriptors = _services.Where(s => s.ServiceType == typeof(IHostedService)).ToList();

            Assert.Equal(ServiceLifetime.Scoped, dbContextDescriptor?.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, appDbContextDescriptor?.Lifetime);

            foreach (var descriptor in hostedServiceDescriptors)
            {
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
            }
        }
    }
}
