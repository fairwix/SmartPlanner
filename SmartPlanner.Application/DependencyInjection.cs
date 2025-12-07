using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SmartPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR для CQRS
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(SmartPlanner.Domain.AssemblyReference).Assembly);

        // Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.Behaviors.ValidationBehavior<,>));

        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // ✅ НЕ регистрируем Generic Repository сервисы здесь
        // ❌ services.AddScoped<IGoalService, GoalService>(); - УДАЛИТЬ если есть

        return services;
    }
}
