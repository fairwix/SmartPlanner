using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Infrastructure.Data;

namespace SmartPlanner.Tests.API.Integration;

public sealed class ApiTestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"SmartPlannerTests-{Guid.NewGuid():N}";
    private readonly string? _previousJwtSecret;

    public ApiTestApplicationFactory()
    {
        // Program validates the JWT key before WebApplicationFactory can add host configuration.
        _previousJwtSecret = Environment.GetEnvironmentVariable("JwtSettings__Secret");
        Environment.SetEnvironmentVariable("JwtSettings__Secret", "integration-test-key-with-more-than-32-bytes-long");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            Environment.SetEnvironmentVariable("JwtSettings__Secret", _previousJwtSecret);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "integration-test-key-with-more-than-32-bytes-long",
                ["CorsSettings:AllowedOrigins:0"] = "https://client.example",
                ["CorsSettings:AllowCredentials"] = "true"
            }));

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(service =>
                         service.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal) == true)
                     .ToArray())
                services.Remove(descriptor);
            services.RemoveAll<IConfigureOptions<DbContextOptions<AppDbContext>>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName, _ => { });
        });
    }
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SmartPlannerTest";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Test-User"], out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var role = Request.Headers["X-Test-Role"].ToString();
        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}
