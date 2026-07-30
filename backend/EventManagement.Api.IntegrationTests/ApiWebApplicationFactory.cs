using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventManagement.Api.IntegrationTests;

internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    internal static readonly string JwtSigningKey = GetJwtSigningKey();
    private readonly Dictionary<string, string?> _originalEnvironment = [];

    public ApiWebApplicationFactory(string connectionString)
    {
        SetEnvironment("ASPNETCORE_ENVIRONMENT", "Testing");
        SetEnvironment("ConnectionStrings__DefaultConnection", connectionString);
        SetEnvironment("Jwt__Issuer", "EventManagement.Api.IntegrationTests");
        SetEnvironment("Jwt__Audience", "EventManagement.Api.IntegrationTests.Client");
        SetEnvironment("Jwt__SigningKey", JwtSigningKey);
        SetEnvironment("Jwt__ExpiryMinutes", "75");
        SetEnvironment("BootstrapAdmin__Email", string.Empty);
        SetEnvironment("BootstrapAdmin__Password", string.Empty);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator, TestGoogleTokenValidator>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        foreach (var (name, value) in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    private void SetEnvironment(string name, string? value)
    {
        _originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    private static string GetJwtSigningKey()
    {
        var configuredKey = Environment.GetEnvironmentVariable("TEST_JWT_SIGNING_KEY");
        if (string.IsNullOrWhiteSpace(configuredKey))
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        if (configuredKey.Length < 32)
            throw new InvalidOperationException(
                "TEST_JWT_SIGNING_KEY must contain at least 32 characters.");
        return configuredKey;
    }
}

internal sealed class TestGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parts = idToken.Split('|');
        if (parts is not [var subject, var email, var name])
            throw new EventManagement.Api.Infrastructure.ApiException(401, "Invalid Google test token.");
        return Task.FromResult(new GoogleIdentity(subject, email, name, null));
    }
}
