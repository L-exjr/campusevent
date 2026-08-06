using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EventManagement.Api.Services;

public sealed class SupabaseImageStorageService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<SupabaseImageStorageService> logger,
    TimeProvider timeProvider) : IImageStorageService
{
    public async Task<StoredImage> UploadImageAsync(
        Stream content,
        string contentType,
        string bucket,
        string extension,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var projectUrl = GetConfigurationValue("Supabase:Url", "SUPABASE_URL");
        var serviceRoleKey = GetConfigurationValue(
            "Supabase:ServiceRoleKey",
            "SUPABASE_SERVICE_ROLE_KEY");

        if (string.IsNullOrWhiteSpace(projectUrl) || string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            logger.LogError("Supabase server-side storage configuration is incomplete.");
            throw new ImageStorageException("Image storage is not configured.");
        }

        var objectPath = $"{ownerId:N}/{timeProvider.GetUtcNow():yyyy-MM-dd}/{Guid.NewGuid():N}.{extension}";
        var endpoint = $"{projectUrl.TrimEnd('/')}/storage/v1/object/{bucket}/{objectPath}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            // The service-role credential is deliberately added only by the backend.
            // It must never be returned to, logged for, or embedded in the frontend.
            request.Headers.Add("apikey", serviceRoleKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
            request.Headers.Add("x-upsert", "false");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            using var response = await httpClientFactory
                .CreateClient(nameof(SupabaseImageStorageService))
                .SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Supabase rejected an upload to bucket {Bucket} with status {StatusCode}.",
                    bucket,
                    (int)response.StatusCode);
                throw new ImageStorageException("The image could not be stored.");
            }

            return new StoredImage(
                objectPath,
                $"{projectUrl.TrimEnd('/')}/storage/v1/object/public/{bucket}/{objectPath}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageStorageException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Supabase upload to bucket {Bucket} failed.", bucket);
            throw new ImageStorageException("The image could not be stored.", exception);
        }
    }

    public async Task DeleteImageAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var projectUrl = GetConfigurationValue("Supabase:Url", "SUPABASE_URL");
        var serviceRoleKey = GetConfigurationValue(
            "Supabase:ServiceRoleKey",
            "SUPABASE_SERVICE_ROLE_KEY");
        if (string.IsNullOrWhiteSpace(projectUrl) || string.IsNullOrWhiteSpace(serviceRoleKey))
            throw new ImageStorageException("Image storage is not configured.");

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{projectUrl.TrimEnd('/')}/storage/v1/object/{bucket}");
            request.Headers.Add("apikey", serviceRoleKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
            request.Content = JsonContent.Create(new { prefixes = new[] { objectKey } });
            using var response = await httpClientFactory
                .CreateClient(nameof(SupabaseImageStorageService))
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Supabase rejected deletion from bucket {Bucket} with status {StatusCode}.",
                    bucket,
                    (int)response.StatusCode);
                throw new ImageStorageException("The image could not be deleted.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageStorageException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Supabase deletion from bucket {Bucket} failed.", bucket);
            throw new ImageStorageException("The image could not be deleted.", exception);
        }
    }

    private string? GetConfigurationValue(string sectionKey, string environmentKey) =>
        configuration[sectionKey] ?? configuration[environmentKey];
}
