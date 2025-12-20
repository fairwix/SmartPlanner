// Application/Authorization/Handlers/ResourceOwnerAuthorizationHandler.cs
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SmartPlanner.Application.Authorization.Requirements;

namespace SmartPlanner.Application.Authorization.Handlers
{
    // УБРАЛИ IUserOwnedResource из дженерика - будет работать через context.Resource
    public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement)
        {
            // Получаем UserId из claims пользователя
            var userIdClaim = context.User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Task.CompletedTask; // Не авторизуем
            }

            // Получаем ресурс из context.Resource
            if (context.Resource is IUserOwnedResource resource)
            {
                if (resource.UserId.ToString() == userIdClaim)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }

    // Оставляем интерфейс в ТОМ ЖЕ файле
    public interface IUserOwnedResource
    {
        Guid UserId { get; }
    }
}
