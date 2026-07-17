using System.Security.Claims;
using EventManagement.Api.Data;
using EventManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Middleware;

public sealed class ActiveUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var idValue = context.User.FindFirstValue(JwtClaimNames.UserId);
            var roleValue = context.User.FindFirstValue(JwtClaimNames.Role);
            if (!Guid.TryParse(idValue, out var userId))
            {
                await RejectAsync(context, "The authentication token is invalid.");
                return;
            }

            var user = await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => new { item.IsActive, item.Role })
                .SingleOrDefaultAsync();

            if (user is null || !user.IsActive)
            {
                await RejectAsync(context, "This account is inactive.");
                return;
            }

            // Role claims are intentionally not refreshed in-place; bounded JWT expiry requires signing in again after a role change.
            if (!string.Equals(user.Role.ToString(), roleValue, StringComparison.Ordinal))
            {
                await RejectAsync(context, "Your role changed. Sign in again to refresh your access.");
                return;
            }
        }

        await next(context);
    }

    private static Task RejectAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { error = message });
    }
}
