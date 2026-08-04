using System.Net;
using EventManagement.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Api.UnitTests.Services;

public sealed class SupabaseImageStorageServiceTests
{
    [Fact]
    public async Task UploadImageAsync_uses_service_role_only_on_server_request()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://example.supabase.co",
                ["Supabase:ServiceRoleKey"] = "server-only-key"
            }).Build();
        var handler = new RecordingHandler();
        var service = new SupabaseImageStorageService(
            configuration,
            new StaticHttpClientFactory(new HttpClient(handler)),
            Mock.Of<ILogger<SupabaseImageStorageService>>());

        await using var content = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        var url = await service.UploadImageAsync(
            content,
            "image/png",
            "profile-images",
            "png",
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("server-only-key", handler.AuthorizationParameter);
        Assert.Equal("server-only-key", handler.ApiKey);
        Assert.Equal("image/png", handler.ContentType);
        Assert.StartsWith(
            "https://example.supabase.co/storage/v1/object/profile-images/11111111111111111111111111111111/",
            handler.RequestUri?.ToString());
        Assert.StartsWith(
            "https://example.supabase.co/storage/v1/object/public/profile-images/11111111111111111111111111111111/",
            url.PublicUrl);
        Assert.EndsWith(".png", url.PublicUrl);
        Assert.StartsWith("11111111111111111111111111111111/", url.ObjectKey);
    }

    [Fact]
    public async Task UploadImageAsync_hides_upstream_failure_details()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["SUPABASE_URL"] = "https://example.supabase.co",
                ["SUPABASE_SERVICE_ROLE_KEY"] = "server-only-key"
            }).Build();
        var service = new SupabaseImageStorageService(
            configuration,
            new StaticHttpClientFactory(new HttpClient(
                new RecordingHandler(HttpStatusCode.Forbidden))),
            Mock.Of<ILogger<SupabaseImageStorageService>>());

        await using var content = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        var exception = await Assert.ThrowsAsync<ImageStorageException>(() =>
            service.UploadImageAsync(
                content,
                "image/png",
                "profile-images",
                "png",
                Guid.NewGuid()));

        Assert.Equal("The image could not be stored.", exception.Message);
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string? ApiKey { get; private set; }
        public string? ContentType { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            ApiKey = request.Headers.GetValues("apikey").Single();
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
