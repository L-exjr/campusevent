using EventManagement.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Api.UnitTests.Services;

public sealed class GmailSmtpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_returns_false_without_app_password()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Gmail:Username"] = "sender@gmail.com",
                ["Email:Gmail:SenderEmail"] = "sender@gmail.com"
            })
            .Build();
        var environment = new Mock<IWebHostEnvironment>();
        var service = new GmailSmtpEmailService(
            configuration,
            new EmailTemplateRenderer(environment.Object),
            new EmailDailySendMonitor(
                configuration,
                TimeProvider.System,
                Mock.Of<ILogger<EmailDailySendMonitor>>(),
                new OperationalMetrics(TimeProvider.System)),
            Mock.Of<ILogger<GmailSmtpEmailService>>());

        var sent = await service.SendEmailAsync(
            "student@example.test",
            "Student",
            "Subject",
            "unused.html",
            new Dictionary<string, string?>());

        Assert.False(sent);
    }
}
