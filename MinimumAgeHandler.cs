using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public MinimumAgeRequirement(int minimumAge) => MinimumAge = minimumAge;

    public int MinimumAge { get; }
}

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var dateOfBirthClaim = context.User.FindFirst(ClaimTypes.DateOfBirth)?.Value;
        if (!string.IsNullOrEmpty(dateOfBirthClaim) && DateTime.TryParse(dateOfBirthClaim, out var dateOfBirth) && dateOfBirth.AddYears(requirement.MinimumAge) <= DateTime.Now)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}