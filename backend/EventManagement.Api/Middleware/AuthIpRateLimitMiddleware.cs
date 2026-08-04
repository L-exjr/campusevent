using EventManagement.Api.Services;

namespace EventManagement.Api.Middleware;

public sealed class AuthIpRateLimitMiddleware(RequestDelegate next)
{
    private static readonly IReadOnlyDictionary<PathString, AuthRateLimitOperation> Operations =
        new Dictionary<PathString, AuthRateLimitOperation>
        {
            ["/api/auth/login"] = AuthRateLimitOperation.Login,
            ["/api/auth/register"] = AuthRateLimitOperation.Registration,
            ["/api/auth/google"] = AuthRateLimitOperation.GoogleLogin,
            ["/api/auth/forgot-password"] = AuthRateLimitOperation.ForgotPassword,
            ["/api/auth/reset-password"] = AuthRateLimitOperation.ResetPassword
        };

    public async Task InvokeAsync(
        HttpContext context,
        IAuthRateLimitService rateLimitService)
    {
        if (HttpMethods.IsPost(context.Request.Method) &&
            Operations.TryGetValue(context.Request.Path, out var operation))
        {
            await rateLimitService.EnsureIpAllowedAsync(
                operation,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.RequestAborted);
        }

        await next(context);
    }
}
