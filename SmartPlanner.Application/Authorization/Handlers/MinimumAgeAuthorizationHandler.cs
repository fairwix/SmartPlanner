using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using SmartPlanner.Application.Authorization.Requirements;

namespace SmartPlanner.Application.Authorization.Handlers
{
    public class MinimumAgeAuthorizationHandler : AuthorizationHandler<MinimumAgeRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MinimumAgeRequirement requirement)
        {
            if (!context.User.HasClaim(c => c.Type == "dateOfBirth"))
            {
                return Task.CompletedTask;
            }

            var dateOfBirthClaim = context.User.FindFirst(c => c.Type == "dateOfBirth")?.Value;

            if (!DateTime.TryParse(dateOfBirthClaim, out var dateOfBirth))
            {
                return Task.CompletedTask;
            }

            var age = CalculateAge(dateOfBirth);

            if (age >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}
