using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Authorization.Requirements
{
    public class ResourceOwnerRequirementHandler :
        AuthorizationHandler<ResourceOwnerRequirement, object>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ResourceOwnerRequirementHandler> _logger;
        private readonly IApplicationDbContext _context;

        public ResourceOwnerRequirementHandler(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ResourceOwnerRequirementHandler> logger,
            IApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement,
            object resource)
        {
            try
            {
                // 1. Получаем ID текущего пользователя из токена
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("userId")?.Value
                    ?? context.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("User ID claim not found in token");
                    context.Fail();
                    return;
                }

                if (!Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    _logger.LogWarning("Invalid User ID format in token: {UserIdClaim}", userIdClaim);
                    context.Fail();
                    return;
                }

                _logger.LogDebug("Current user ID from token: {CurrentUserId}", currentUserId);

                // 2. В зависимости от типа ресурса проверяем владение
                switch (resource)
                {
                    case Domain.Entities.Goal goal:
                        await HandleGoalRequirement(context, requirement, goal, currentUserId);
                        break;

                    // Добавьте другие типы ресурсов по мере необходимости
                    // case Domain.Entities.Task task:
                    //     await HandleTaskRequirement(context, requirement, task, currentUserId);
                    //     break;

                    default:
                        _logger.LogWarning("Unsupported resource type: {ResourceType}", resource.GetType());
                        context.Fail();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResourceOwnerRequirementHandler");
                context.Fail();
            }
        }

        private async Task HandleGoalRequirement(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement,
            Domain.Entities.Goal goal,
            Guid currentUserId)
        {
            _logger.LogDebug("Checking ownership for goal {GoalId}. Goal UserId: {GoalUserId}, Current UserId: {CurrentUserId}",
                goal.Id, goal.UserId, currentUserId);

            // 3. Проверяем, что пользователь является владельцем цели
            if (goal.UserId == currentUserId)
            {
                _logger.LogDebug("User {CurrentUserId} is owner of goal {GoalId}", currentUserId, goal.Id);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("User {CurrentUserId} is NOT owner of goal {GoalId}. Goal owner: {GoalUserId}",
                    currentUserId, goal.Id, goal.UserId);
                context.Fail();
            }
        }
    }
}
