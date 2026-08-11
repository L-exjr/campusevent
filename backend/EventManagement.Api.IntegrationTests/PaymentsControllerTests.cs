using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class PaymentsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Paid_event_cannot_be_registered_through_the_free_endpoint()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("paid-bypass@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Protected paid event", 5, 12_500);
        using var client = CreateAuthenticatedClient(student.Token);

        using var response = await client.PostAsync($"/api/events/{eventId}/register", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));
    }

    [Fact]
    public async Task Paid_event_requires_verified_webhook_before_registration()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("paid-registration@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Paid event", 5, 12_500);
        using var client = CreateAuthenticatedClient(student.Token);

        using var initialize = await client.PostAsync(
            $"/api/payments/events/{eventId}/initialize",
            null);
        initialize.EnsureSuccessStatusCode();
        var reference = (await ReadJsonAsync(initialize)).GetProperty("reference").GetString()!;

        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));
        Assert.Equal(1, await Fixture.CountPaymentOrdersAsync(eventId));

        using var webhook = Fixture.CreateClient();
        webhook.DefaultRequestHeaders.Add("x-paystack-signature", "valid-test-signature");
        using var first = await webhook.PostAsJsonAsync(
            "/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });
        using var duplicate = await webhook.PostAsJsonAsync(
            "/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(1, await Fixture.CountRegistrationsAsync(eventId));
        Assert.Equal(1, await Fixture.CountEmailOutboxMessagesAsync("RegistrationConfirmation"));
        using var status = await client.GetAsync($"/api/payments/{reference}");
        status.EnsureSuccessStatusCode();
        Assert.Equal("Verified", (await ReadJsonAsync(status)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Pending_paid_checkout_reserves_the_last_place()
    {
        await ResetAsync();
        var admin = await LoginAdminAsync();
        var payingStudent = await RegisterStudentAsync("reserved-seat@example.test");
        var otherStudent = await RegisterStudentAsync("free-path@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Reserved paid event", 1, 5_000);
        using var payingClient = CreateAuthenticatedClient(payingStudent.Token);
        using var otherClient = CreateAuthenticatedClient(otherStudent.Token);

        using var initialized = await payingClient.PostAsync(
            $"/api/payments/events/{eventId}/initialize",
            null);
        using var competing = await otherClient.PostAsync($"/api/events/{eventId}/register", null);

        initialized.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, competing.StatusCode);
        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));
    }

    [Fact]
    public async Task Paystack_webhook_rejects_invalid_signature()
    {
        await ResetAsync();
        using var client = Fixture.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference = "unknown" } });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
