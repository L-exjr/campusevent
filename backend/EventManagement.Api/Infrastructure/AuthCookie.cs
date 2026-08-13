namespace EventManagement.Api.Infrastructure;

public static class AuthCookie
{
    public const string Name = "campus_events_session";

    public static CookieOptions Options(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = expiresAt,
        Path = "/",
        IsEssential = true
    };
}
