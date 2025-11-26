using Microsoft.Extensions.Options;
using SmartPlanner.Application;
using SmartPlanner.Infrastructure;
using SmartPlanner.Infrastructure.Configuration;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Planner API",
        Version = "v1",
        Description = "API для управления целями, челленджами и достижениями"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Planner API V1");
        c.RoutePrefix = string.Empty; // Чтобы Swagger открывался на корневом URL
    });
}

app.UseRouting();

// ✅ ПРАВИЛЬНЫЙ ПОРЯДОК Middleware:
app.UseCors("AllowAll");
app.UseAuthorization();

// ✅ Добавьте эти важные middleware:
app.UseHttpsRedirection(); // важно для перенаправления HTTP->HTTPS

app.MapControllers();

app.UseMiddleware<SmartPlanner.API.Middleware.GlobalExceptionHandlingMiddleware>();

// ✅ Создание директории через Options Pattern
using (var scope = app.Services.CreateScope())
{
    var options = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

    if (!Directory.Exists(options.DataDirectory))
    {
        Directory.CreateDirectory(options.DataDirectory);
        app.Logger.LogInformation("Created data directory: {DataDirectory}", options.DataDirectory);
    }
}

app.Logger.LogInformation("Smart Planner API started successfully!");
app.Run();
