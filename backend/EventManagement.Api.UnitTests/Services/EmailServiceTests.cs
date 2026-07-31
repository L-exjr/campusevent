using System.Net;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Api.UnitTests.Services;

public sealed class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_uses_mailtrap_sandbox_api_when_token_is_configured()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"email-service-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(contentRoot, "EmailTemplates"));
        await File.WriteAllTextAsync(
            Path.Combine(contentRoot, "EmailTemplates", "Test.html"),
            "<p>Hello {{Name}}</p>");

        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Email:Api:Token"] = "test-api-token",
                    ["Email:Api:InboxId"] = "12345",
                    ["Email:Api:UseSandbox"] = "true",
                    ["Email:Smtp:FromAddress"] = "notifications@example.test",
                    ["Email:Smtp:FromName"] = "Campus Events"
                }).Build();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);
            environment.SetupGet(item => item.ContentRootPath).Returns(contentRoot);
            var handler = new RecordingHandler();
            var service = new EmailService(
                configuration,
                environment.Object,
                new StaticHttpClientFactory(new HttpClient(handler)),
                Mock.Of<ILogger<EmailService>>());

            var sent = await service.SendAsync(
                "student@example.test",
                "Test Student",
                "Test subject",
                "Test.html",
                new Dictionary<string, string?> { ["Name"] = "Test Student" });

            Assert.True(sent);
            Assert.Equal(
                "https://sandbox.api.mailtrap.io/api/send/12345",
                handler.RequestUri?.ToString());
            Assert.Equal("test-api-token", handler.ApiToken);
            Assert.Contains("Hello Test Student", handler.Body);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? ApiToken { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            ApiToken = request.Headers.GetValues("Api-Token").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
