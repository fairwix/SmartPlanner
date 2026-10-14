using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SmartPlanner.Application.Authorization.Requirements;

namespace SmartPlanner.Application.Authorization.Handlers
{
    public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Task.CompletedTask;
            }

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

    public interface IUserOwnedResource
    {
        Guid UserId { get; }
    }
}
