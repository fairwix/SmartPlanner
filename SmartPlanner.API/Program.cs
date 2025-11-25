// Program.cs

using Microsoft.Extensions.Options;
using SmartPlanner.Application;
using SmartPlanner.Infrastructure;
using SmartPlanner.Infrastructure.Configuration; // ✅ Добавляем using
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
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// ✅ ПРАВИЛЬНОЕ создание директории через Options Pattern
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