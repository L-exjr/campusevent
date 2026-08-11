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
        SetEnvironment("QR_SIGNING_KEY", "integration-test-ticket-signing-key-which-is-long-enough");
        SetEnvironment("Payments__OrganizerSubaccountsEnabled", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator, TestGoogleTokenValidator>();
            services.RemoveAll<IImageStorageService>();
            services.AddSingleton<IImageStorageService, TestImageStorageService>();
            services.RemoveAll<IPaystackPaymentProvider>();
            services.AddSingleton<IPaystackPaymentProvider, TestPaystackPaymentProvider>();
            services.RemoveAll<ICertificateStorageService>();
            services.AddSingleton<ICertificateStorageService, TestCertificateStorageService>();
            services.RemoveAll<ICertificatePdfGenerator>();
            services.AddSingleton<ICertificatePdfGenerator, TestCertificatePdfGenerator>();
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

internal sealed class TestCertificateStorageService : ICertificateStorageService
{
    private readonly Dictionary<string, byte[]> _objects = [];

    public Task UploadAsync(string objectKey, byte[] pdf, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _objects[objectKey] = pdf;
        return Task.CompletedTask;
    }

    public Task<CertificateSignedUrl> CreateSignedUrlAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_objects.ContainsKey(objectKey))
            throw new InvalidOperationException("The test certificate was not uploaded.");
        return Task.FromResult(new CertificateSignedUrl(
            $"https://storage.example.test/signed/{objectKey}",
            DateTimeOffset.UtcNow.AddMinutes(5)));
    }
}

internal sealed class TestCertificatePdfGenerator : ICertificatePdfGenerator
{
    public byte[] Generate(CertificatePdfModel model) =>
        System.Text.Encoding.UTF8.GetBytes($"test-pdf:{model.RegistrationId:N}");
}

internal sealed class TestPaystackPaymentProvider : IPaystackPaymentProvider
{
    private readonly Dictionary<string, PaystackVerification> _payments = [];

    public bool HasValidSignature(string payload, string? signature) =>
        signature == "valid-test-signature";

    public Task<PaystackInitialization> InitializeAsync(
        string email,
        long amountMinor,
        string currency,
        string reference,
        string callbackUrl,
        Guid orderId,
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _payments[reference] = new PaystackVerification(
            true,
            reference,
            amountMinor,
            currency);
        return Task.FromResult(new PaystackInitialization(
            $"https://checkout.example.test/{reference}",
            reference));
    }

    public Task<PaystackVerification> VerifyAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_payments[reference]);
    }

    public Task<bool> RequestRefundAsync(
        string reference,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }
}

internal sealed class TestImageStorageService : IImageStorageService
{
    public Task<StoredImage> UploadImageAsync(
        Stream content,
        string contentType,
        string bucket,
        string extension,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = $"{ownerId:N}/{Guid.NewGuid():N}.{extension}";
        return Task.FromResult(new StoredImage(key, $"https://storage.example.test/{bucket}/{key}"));
    }

    public Task DeleteImageAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
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
