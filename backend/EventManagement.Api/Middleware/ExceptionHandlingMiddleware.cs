using EventManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Antiforgery;

namespace EventManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "The CSRF token is missing or invalid." });
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "A database constraint rejected the request.");
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "The request conflicts with existing data." });
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            logger.LogWarning(exception, "A serializable transaction conflicted with another request.");
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "The data changed during this request. Please retry." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unhandled API error occurred.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected server error occurred." });
        }
    }
}
