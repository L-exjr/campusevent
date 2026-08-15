namespace EventManagement.Api.Infrastructure;

public static class AuthCookie
{
    public const string Name = "campus_events_session";

    public static CookieOptions Options(DateTimeOffset expiresAt, bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
        Expires = expiresAt,
        Path = "/",
        IsEssential = true
    };
}
