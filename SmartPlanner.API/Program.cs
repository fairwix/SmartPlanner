using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartPlanner.API.Configuration;
using SmartPlanner.API.Filters;
using SmartPlanner.API.Middleware;
using SmartPlanner.Application;
using SmartPlanner.Application.Common.Interfaces;
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

// Настройки
builder.WebHost.UseUrls("http://localhost:5047");

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 2_000_000_000;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
    options.MultipartHeadersCountLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue;
    options.Limits.MaxRequestBufferSize = null;
    options.Limits.MaxRequestLineSize = 16 * 1024;
    options.Limits.MaxRequestHeaderCount = 128;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = long.MaxValue;
    options.AllowSynchronousIO = true;
});

// 1. DbContext (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
    ));

// Регистрация IApplicationDbContext как интерфейса
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// 2. MediatR + FluentValidation
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
        var secret = jwtSettings["Secret"] ?? "dev-secret-key-123456789012345678901234567890";
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
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
// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // React dev server
                "http://localhost:5173",  // Vite dev server
                "http://localhost:4200",  // Angular dev server
                "http://localhost:5047",  // Твой API
                "http://localhost:5000",  // Дополнительные порты
                "http://localhost:8080"
            )
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true); // Разрешить все origins (для разработки)
    });
});

// 8. MemoryCache (для RateLimit и других нужд)
builder.Services.AddMemoryCache();

// 🔥 НОВОЕ: Rate Limiting для конкретных эндпоинтов
builder.Services.AddRateLimiter(options =>
{
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

app.UseRateLimiter();

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<CorsLoggingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        Console.WriteLine("✅ Миграции применены успешно.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка применения миграций: {ex.Message}");
    }
}


await app.RunAsync();
