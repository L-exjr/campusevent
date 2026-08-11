using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Api.Services;

public sealed record CertificateSignedUrl(string Url, DateTimeOffset ExpiresAt);

public interface ICertificateStorageService
{
    Task UploadAsync(string objectKey, byte[] pdf, CancellationToken cancellationToken);
    Task<CertificateSignedUrl> CreateSignedUrlAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed class SupabaseCertificateStorageService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    TimeProvider timeProvider) : ICertificateStorageService
{
    public async Task UploadAsync(string objectKey, byte[] pdf, CancellationToken cancellationToken)
    {
        var settings = GetSettings();
        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"storage/v1/object/{EscapePath(settings.Bucket)}/{EscapePath(objectKey)}");
        request.Headers.TryAddWithoutValidation("x-upsert", "true");
        request.Content = new ByteArrayContent(pdf);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateStorageExceptionAsync(response, "upload", cancellationToken);
    }

    public async Task<CertificateSignedUrl> CreateSignedUrlAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        var settings = GetSettings();
        using var request = CreateRequest(
            HttpMethod.Post,
            settings,
            $"storage/v1/object/sign/{EscapePath(settings.Bucket)}/{EscapePath(objectKey)}");
        request.Content = JsonContent.Create(new { expiresIn = settings.SignedUrlSeconds });

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateStorageExceptionAsync(response, "create a download link", cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var signedUrl = document.RootElement.TryGetProperty("signedURL", out var value)
            ? value.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(signedUrl))
            throw new InvalidOperationException("Supabase did not return a certificate download URL.");
        var absoluteUrl = Uri.TryCreate(signedUrl, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : new Uri(
                settings.ProjectUrl,
                signedUrl.StartsWith("/storage/v1/", StringComparison.Ordinal)
                    ? signedUrl.TrimStart('/')
                    : $"storage/v1/{signedUrl.TrimStart('/')}").ToString();
        return new CertificateSignedUrl(
            absoluteUrl,
            timeProvider.GetUtcNow().AddSeconds(settings.SignedUrlSeconds));
    }

    private StorageSettings GetSettings()
    {
        var url = configuration["SUPABASE_URL"];
        if (string.IsNullOrWhiteSpace(url)) url = configuration["Supabase:Url"];
        var key = configuration["SUPABASE_SERVICE_ROLE_KEY"];
        if (string.IsNullOrWhiteSpace(key)) key = configuration["Supabase:ServiceRoleKey"];
        if (!Uri.TryCreate(url?.TrimEnd('/') + "/", UriKind.Absolute, out var projectUrl) ||
            string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Certificate storage requires a valid SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY.");
        var bucket = configuration["CERTIFICATES_BUCKET"];
        if (string.IsNullOrWhiteSpace(bucket)) bucket = configuration["Certificates:Bucket"] ?? "certificates";
        var minutesText = configuration["CERTIFICATE_SIGNED_URL_MINUTES"];
        var minutes = int.TryParse(minutesText, out var parsed)
            ? parsed
            : configuration.GetValue("Certificates:SignedUrlMinutes", 60);
        minutes = Math.Clamp(minutes, 1, 60);
        return new StorageSettings(projectUrl, key, bucket, minutes * 60);
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        StorageSettings settings,
        string path)
    {
        var request = new HttpRequestMessage(method, new Uri(settings.ProjectUrl, path));
        request.Headers.TryAddWithoutValidation("apikey", settings.ServiceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ServiceRoleKey);
        return request;
    }

    private static async Task<Exception> CreateStorageExceptionAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (detail.Length > 500) detail = detail[..500];
        return new InvalidOperationException(
            $"Supabase could not {operation} the certificate (HTTP {(int)response.StatusCode}): {detail}");
    }

    private static string EscapePath(string path) => string.Join('/',
        path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

    private sealed record StorageSettings(
        Uri ProjectUrl,
        string ServiceRoleKey,
        string Bucket,
        int SignedUrlSeconds);
}
