// SmartPlanner.API/Program.cs - УЛЬТРА-ПРОСТАЯ
using FluentMigrator.Runner;
using SmartPlanner.Application;
using SmartPlanner.Infrastructure;
using SmartPlanner.API.Configuration;
using SmartPlanner.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем всё необходимое
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // ✅ Без параметров!

// 2. Добавляем наши сервисы
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// 3. Миграции (пропустим если ошибки)
try
{
    using var scope = app.Services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    if (migrationRunner.HasMigrationsToApplyUp())
    {
        app.Logger.LogInformation("Applying migrations...");
        migrationRunner.MigrateUp();
    }
}
catch { } // Игнорируем ошибки миграций

// 4. Включаем Swagger ВСЕГДА
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Planner API");
    c.RoutePrefix = ""; // Swagger на главной
});

// 5. Включаем всё остальное
app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

// 6. Добавляем тестовый endpoint
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
app.MapGet("/health", () => "API работает! ✅");
app.MapGet("/test", () => new {
    message = "API готов к работе",
    time = DateTime.Now
});

app.Logger.LogInformation("=== API запущен ===");
app.Logger.LogInformation("Swagger доступен: http://localhost:5047");
app.Logger.LogInformation("Тестовый endpoint: http://localhost:5047/test");

app.Run();
