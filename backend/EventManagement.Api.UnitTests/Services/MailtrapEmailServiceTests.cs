using System.Net;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Api.UnitTests.Services;

public sealed class MailtrapEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_uses_mailtrap_sending_api()
    {
        var fixture = await EmailFixture.CreateAsync(includeConfiguration: true);
        await using (fixture)
        {
            var sent = await fixture.Service.SendEmailAsync(
                "student@example.test",
                "Test Student",
                "Test subject",
                "Test.html",
                new Dictionary<string, string?> { ["Name"] = "Test Student" });

            Assert.True(sent);
            Assert.Equal("https://send.api.mailtrap.io/api/send", fixture.Handler.RequestUri?.ToString());
            Assert.Equal("Bearer", fixture.Handler.AuthorizationScheme);
            Assert.Equal("test-api-token", fixture.Handler.AuthorizationParameter);
            Assert.Contains("hello@demomailtrap.co", fixture.Handler.Body);
            Assert.Contains("Hello Test Student", fixture.Handler.Body);
        }
    }

    [Fact]
    public async Task SendEmailAsync_returns_false_when_configuration_is_missing()
    {
        var fixture = await EmailFixture.CreateAsync(includeConfiguration: false);
        await using (fixture)
        {
            var sent = await fixture.Service.SendEmailAsync(
                "student@example.test",
                "Test Student",
                "Test subject",
                "Test.html",
                new Dictionary<string, string?> { ["Name"] = "Test Student" });

            Assert.False(sent);
            Assert.Equal(0, fixture.Handler.CallCount);
        }
    }

    private sealed class EmailFixture : IAsyncDisposable
    {
        private EmailFixture(
            string contentRoot,
            MailtrapEmailService service,
            RecordingHandler handler)
        {
            ContentRoot = contentRoot;
            Service = service;
            Handler = handler;
        }

        public string ContentRoot { get; }
        public MailtrapEmailService Service { get; }
        public RecordingHandler Handler { get; }

        public static async Task<EmailFixture> CreateAsync(bool includeConfiguration)
        {
            var contentRoot = Path.Combine(Path.GetTempPath(), $"email-service-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(contentRoot, "EmailTemplates"));
            await File.WriteAllTextAsync(
                Path.Combine(contentRoot, "EmailTemplates", "Test.html"),
                "<p>Hello {{Name}}</p>");

            var values = includeConfiguration
                ? new Dictionary<string, string?>
                {
                    ["Email:Api:Token"] = "test-api-token",
                    ["Email:Api:SenderEmail"] = "hello@demomailtrap.co"
                }
                : new Dictionary<string, string?>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);
            environment.SetupGet(item => item.ContentRootPath).Returns(contentRoot);
            var handler = new RecordingHandler();
            var service = new MailtrapEmailService(
                configuration,
                new EmailTemplateRenderer(environment.Object),
                new StaticHttpClientFactory(new HttpClient(handler)),
                Mock.Of<ILogger<MailtrapEmailService>>());
            return new EmailFixture(contentRoot, service, handler);
        }

        public ValueTask DisposeAsync()
        {
            Directory.Delete(ContentRoot, recursive: true);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StaticHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
