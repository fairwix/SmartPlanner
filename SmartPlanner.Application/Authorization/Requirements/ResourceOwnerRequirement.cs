using Microsoft.AspNetCore.Authorization;

namespace SmartPlanner.Application.Authorization.Requirements
{
    // Это пустой класс-маркер. Сама логика проверки будет в Handler.
    public class ResourceOwnerRequirement : IAuthorizationRequirement { }
}
