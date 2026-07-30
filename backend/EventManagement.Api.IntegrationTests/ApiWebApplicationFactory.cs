using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventManagement.Api.IntegrationTests;

internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    internal static readonly string JwtSigningKey =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
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
}
