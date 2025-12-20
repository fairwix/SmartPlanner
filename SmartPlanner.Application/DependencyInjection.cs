using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartPlanner.Application.Auth.Interfaces;
using SmartPlanner.Application.Auth.Services;
using SmartPlanner.Application.Common.Behaviors;
using SmartPlanner.Application.Security.Services;
using SmartPlanner.Application.Services;

namespace SmartPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR для CQRS
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // FluentValidation (простая регистрация)
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Регистрируем сервисы безопасности
        services.AddScoped<IAuditService, AuditService>();

        // Background services
        services.AddHostedService<AuditLogCleanupService>();

        services.AddScoped<IConfirmationTokenService, ConfirmationTokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        // Background services
        services.AddHostedService<AuditLogCleanupService>();

        // Email cleanup service (новый)
        services.AddHostedService<EmailTokenCleanupService>();


        return services;
    }
}
