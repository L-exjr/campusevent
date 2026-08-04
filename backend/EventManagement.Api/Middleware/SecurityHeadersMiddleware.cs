namespace EventManagement.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.Append("Referrer-Policy", "no-referrer");
        headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        headers.Append(
            "Content-Security-Policy",
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
        return next(context);
    }
}
