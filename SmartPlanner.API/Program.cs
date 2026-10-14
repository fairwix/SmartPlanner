using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartPlanner.API.Configuration;
using SmartPlanner.API.Filters;
using SmartPlanner.API.Middleware;
using SmartPlanner.Application;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Common.Security;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Infrastructure;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using SmartPlanner.Application.Authorization.Requirements;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Services;
using SmartPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using SmartPlanner.API.Hubs;
using SmartPlanner.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Refuse to start with a missing, short, or repository-known JWT signing key.
var jwtSecret = JwtSecretValidator.GetRequiredSecret(builder.Configuration);

// Настройки
builder.WebHost.UseUrls("http://localhost:5047");

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("CorsSettings:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();
    var allowCredentials = builder.Configuration.GetValue("CorsSettings:AllowCredentials", true);

    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (allowCredentials)
            policy.AllowCredentials();
    });
});


const long MaxUploadRequestBytes = 2_100_000_000;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = MaxUploadRequestBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadRequestBytes;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = MaxUploadRequestBytes;
});

// 1. MediatR + FluentValidation
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Контроллеры + JSON
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true; // Подробные ошибки для разработки
        options.KeepAliveInterval = TimeSpan.FromSeconds(60); // Keep-alive каждые 15 секунд
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(120); // Таймаут клиента 30 секунд
        options.HandshakeTimeout = TimeSpan.FromSeconds(60); // Таймаут handshake

        options.MaximumParallelInvocationsPerClient = 10;
        // Максимальное количество сообщений в буфере
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB

        // Настройка повторного подключения
        options.MaximumParallelInvocationsPerClient = 1;

        options.AddFilter<SignalRRateLimitFilter>();
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 4. Swagger с JWT-авторизацией
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartPlanner API", Version = "v1" });

    // Определение схемы авторизации
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer [token]'"
    });

    // Глобальное требование авторизации для всех эндпоинтов (опционально)
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 5. JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var issuer = jwtSettings["Issuer"] ?? "smartplanner-dev";
        var audience = jwtSettings["Audience"] ?? "smartplanner-dev-clients";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // Если это запрос к SignalR хабу
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/notifications") ||
                     path.StartsWithSegments("/hubs/file")))
                {
                    // Используем токен из query string для WebSocket
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// 6. Авторизация + политики
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ResourceOwner", policy =>
        policy.Requirements.Add(new ResourceOwnerRequirement()));

    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));

    options.AddPolicy("RequireEmailConfirmed", policy =>
        policy.RequireClaim("emailConfirmed", "True"));

    options.AddPolicy("RequirePremiumSubscription", policy =>
        policy.RequireClaim("SubscriptionLevel", "Premium", "Enterprise"));

    options.AddPolicy("RequireMinimumAge18", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));

    options.AddPolicy("CanEditGoal", policy =>
        policy.RequireClaim("permission", "Goal.Edit"));

    options.AddPolicy("CanDeleteUser", policy =>
        policy.RequireClaim("permission", "User.Delete"));
});

builder.Services.AddSingleton<SignalRRateLimitFilter>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerRequirementHandler>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileNotificationService, FileNotificationService>();
// 8. MemoryCache (для RateLimit и других нужд)
builder.Services.AddMemoryCache();

// 🔥 НОВОЕ: Rate Limiting для конкретных эндпоинтов
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LargeFileUploadConcurrency", _ =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: "large-file-uploads",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 2,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    // Политика для загрузки файлов: 10 загрузок в минуту на пользователя
    options.AddPolicy("FileUploadLimit", context =>
    {
        // Применяем только к эндпоинтам загрузки файлов
        if (context.Request.Path.StartsWithSegments("/api/files") &&
            (context.Request.Method == "POST" || context.Request.Method == "PUT"))
        {
            var userId = context.User?.Identity?.Name ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"file-upload-{userId}",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10, // 10 загрузок в минуту
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0 // Не ставим в очередь, сразу 429
                });
        }
        return RateLimitPartition.GetNoLimiter("default");
    });

    // Политика для SignalR хаба
    options.AddPolicy("SignalRLimit", context =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            var userId = context.User?.Identity?.Name ?? "anonymous";
            return RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"signalr-{userId}",
                factory: partition => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5, // 5 подключений
                    TokensPerPeriod = 1,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(30),
                    QueueLimit = 0
                });
        }
        return RateLimitPartition.GetNoLimiter("default");
    });
});

// 9. HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 10. Кастомные сервисы
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<CorsLoggingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("ConfiguredOrigins");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHub<FileHub>("/hubs/file");
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapControllers();

// Swagger — только в Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartPlanner API v1");
        c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
}

// Health checks
app.MapGet("/", () => "SmartPlanner API работает!");
app.MapGet("/health", () => "OK");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}


await app.RunAsync();

public partial class Program { }
