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
using Microsoft.AspNetCore.Authorization;
using SmartPlanner.Application.Authorization.Requirements;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Services;
using SmartPlanner.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Настройки
builder.WebHost.UseUrls("http://localhost:5047");

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

builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerRequirementHandler>();
builder.Services.AddScoped<IFileService, FileService>();

// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 8. MemoryCache (для RateLimit и других нужд)
builder.Services.AddMemoryCache();

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
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

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
