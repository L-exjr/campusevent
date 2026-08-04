using EventManagement.Api.Services;

namespace EventManagement.Api.UnitTests.Services;

public sealed class EmailOutboxPayloadTests
{
    [Fact]
    public void Payload_round_trips_for_durable_delivery()
    {
        var source = new EmailOutboxPayload(
            "student@example.test",
            "Student",
            "Reset password",
            "PasswordReset.html",
            new Dictionary<string, string?> { ["ResetUrl"] = "https://example.test/reset?token=secret" });
        var json = System.Text.Json.JsonSerializer.Serialize(source);

        var restored = EmailOutbox.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(source.RecipientEmail, restored.RecipientEmail);
        Assert.Equal(source.TemplateValues["ResetUrl"], restored.TemplateValues["ResetUrl"]);
    }
}
