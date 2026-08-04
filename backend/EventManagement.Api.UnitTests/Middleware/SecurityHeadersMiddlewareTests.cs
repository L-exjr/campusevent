using EventManagement.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace EventManagement.Api.UnitTests.Middleware;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Adds_API_security_headers()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Contains(
            "frame-ancestors 'none'",
            context.Response.Headers["Content-Security-Policy"].ToString());
    }
}
