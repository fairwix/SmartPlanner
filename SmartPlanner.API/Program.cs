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
using SmartPlanner.Application.Authorization.Requirements;
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
builder.Services.AddApplication(); // Предполагается, что у тебя есть метод AddApplication() в Application layer
builder.Services.AddInfrastructure(builder.Configuration); // Предполагается, что у тебя есть AddInfrastructure()

// Если нет — замени на:
//builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
// builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// 3. Контроллеры + JSON
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>(); // Глобальный фильтр исключений
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

    // Добавляем поддержку JWT в Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer [token]'"
    });
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("12345678901234567890123456789012"))
        };
    });

// 6. Авторизация + политики
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireRole("Admin"));
    // Добавь Resource-based policy, если используешь AuthorizeAsync с "ResourceOwner"

    // 2. Требуется подтверждённый email
    options.AddPolicy("RequireEmailConfirmed", policy =>
        policy.RequireClaim("emailConfirmed", "True"));

    // 3. Премиум подписка
    options.AddPolicy("RequirePremiumSubscription", policy =>
        policy.RequireClaim("SubscriptionLevel", "Premium", "Enterprise"));

    // 4. Минимум 18 лет (через custom requirement)
    options.AddPolicy("RequireMinimumAge18", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));

    // 5. Permissions через claims (из JWT)
    options.AddPolicy("CanEditGoal", policy =>
        policy.RequireClaim("permission", "Goal.Edit"));

    options.AddPolicy("CanDeleteUser", policy =>
        policy.RequireClaim("permission", "User.Delete"));

});

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

// 8. MemoryCache (для RateLimit)
builder.Services.AddMemoryCache();

// 9. HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 10. Кастомные сервисы
builder.Services.AddScoped<IAuditService, AuditService>(); // Убедись, что AuditService существует
// Добавь другие сервисы по необходимости

// -------------------
// Сборка приложения
// -------------------
var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // Глобальный middleware для ошибок
app.UseMiddleware<CorsLoggingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health checks (опционально)
app.MapGet("/", () => "SmartPlanner API работает!");
app.MapGet("/health", () => "OK");

await app.RunAsync();
