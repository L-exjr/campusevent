using System.Security.Claims;
using EventManagement.Api.Models;

namespace EventManagement.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtClaimNames.UserId);
        if (!Guid.TryParse(value, out var userId))
            throw new ApiException(StatusCodes.Status401Unauthorized, "The authentication token is invalid.");
        return userId;
    }

    public static UserRole GetRequiredRole(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtClaimNames.Role);
        if (!Enum.TryParse<UserRole>(value, out var role))
            throw new ApiException(StatusCodes.Status401Unauthorized, "The authentication token has no valid role.");
        return role;
    }
}
