using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class PaymentsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Ticket_tier_and_server_side_coupon_discount_flow_to_active_provider()
    {
        await ResetAsync();
        var owner = await RegisterStudentAsync("tier-owner@example.test");
        var student = await RegisterStudentAsync("tier-student@example.test");
        using var ownerClient = CreateAuthenticatedClient(owner.Token);
        using var create = await ownerClient.PostAsJsonAsync("/api/events", new
        {
            title = "Tiered paid event", description = "A paid event with VIP and Regular ticket tiers.",
            date = DateTimeOffset.UtcNow.AddDays(7), location = "Tier Hall", capacity = 15,
            category = "Startup & Tech", ticketingEnabled = true, registrationsEnabled = false,
            priceMinor = 10_000, currency = "GHS", salesStartsAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            salesEndsAt = DateTimeOffset.UtcNow.AddDays(6),
            ticketTiers = new[]
            {
                new { name = "VIP", priceMinor = 20_000, capacity = 5 },
                new { name = "Regular", priceMinor = 10_000, capacity = 10 }
            }
        });
        create.EnsureSuccessStatusCode();
        var eventBody = await ReadJsonAsync(create);
        var eventId = eventBody.GetProperty("id").GetGuid();
        var vipId = eventBody.GetProperty("ticketTiers")[0].GetProperty("id").GetGuid();
        using var coupon = await ownerClient.PostAsJsonAsync("/api/coupons", new
        {
            code = "VIP25", percentageDiscount = 25, usageLimit = 2,
            eventId, isActive = true
        });
        coupon.EnsureSuccessStatusCode();
        using var studentClient = CreateAuthenticatedClient(student.Token);
        using var initialize = await studentClient.PostAsync(
            $"/api/payments/events/{eventId}/initialize?ticketTierId={vipId}&couponCode=vip25", null);
        initialize.EnsureSuccessStatusCode();
        var payment = await ReadJsonAsync(initialize);
        Assert.Equal(20_000, payment.GetProperty("originalAmountMinor").GetInt64());
        Assert.Equal(5_000, payment.GetProperty("discountAmountMinor").GetInt64());
        Assert.Equal(15_000, payment.GetProperty("amountMinor").GetInt64());
        Assert.Equal("VIP25", payment.GetProperty("couponCode").GetString());
    }

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
        Assert.True(initialize.IsSuccessStatusCode, await initialize.Content.ReadAsStringAsync());
        var reference = (await ReadJsonAsync(initialize)).GetProperty("reference").GetString()!;

        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));
        Assert.Equal(1, await Fixture.CountPaymentOrdersAsync(eventId));

        using var unverifiedWebhook = Fixture.CreateClient();
        using var unverified = await unverifiedWebhook.PostAsJsonAsync(
            "/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });

        Assert.Equal(HttpStatusCode.Unauthorized, unverified.StatusCode);
        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));

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
        using var competing = await otherClient.PostAsync(
            $"/api/payments/events/{eventId}/initialize",
            null);

        Assert.True(initialized.IsSuccessStatusCode, await initialized.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, competing.StatusCode);
        Assert.Equal(
            "This event is at capacity.",
            (await ReadJsonAsync(competing)).GetProperty("error").GetString());
        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));
        Assert.Equal(1, await Fixture.CountPaymentOrdersAsync(eventId));
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

    [Fact]
    public async Task Flutterwave_verified_webhook_registers_once_and_records_provider()
    {
        await ResetAsync();
        Fixture.UsePaymentProvider("Flutterwave");
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("flutterwave-registration@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Flutterwave sandbox event", 5, 12_500);
        using var client = CreateAuthenticatedClient(student.Token);

        using var initialize = await client.PostAsync($"/api/payments/events/{eventId}/initialize", null);
        Assert.True(initialize.IsSuccessStatusCode, await initialize.Content.ReadAsStringAsync());
        var reference = (await ReadJsonAsync(initialize)).GetProperty("reference").GetString()!;

        using var webhook = Fixture.CreateClient();
        webhook.DefaultRequestHeaders.Add("verif-hash", "valid-flutterwave-signature");
        var notification = new
        {
            @event = "charge.completed",
            data = new { status = "successful", tx_ref = reference }
        };
        using var first = await webhook.PostAsJsonAsync("/api/payments/webhooks/flutterwave", notification);
        using var duplicate = await webhook.PostAsJsonAsync("/api/payments/webhooks/flutterwave", notification);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(1, await Fixture.CountRegistrationsAsync(eventId));
        Assert.Equal(1, await Fixture.CountEmailOutboxMessagesAsync("RegistrationConfirmation"));
    }

    [Fact]
    public async Task Flutterwave_webhook_rejects_invalid_signature()
    {
        await ResetAsync();
        using var client = Fixture.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/payments/webhooks/flutterwave",
            new { @event = "charge.completed", data = new { status = "successful", tx_ref = "unknown" } });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Flutterwave_failed_verification_does_not_register_attendee()
    {
        await ResetAsync();
        Fixture.UsePaymentProvider("Flutterwave");
        Fixture.SetFlutterwaveVerificationResult(false);
        var admin = await LoginAdminAsync();
        var student = await RegisterStudentAsync("flutterwave-failure@example.test");
        var eventId = await CreateEventAsync(admin.Token, "Flutterwave failed payment", 5, 9_000);
        using var client = CreateAuthenticatedClient(student.Token);
        using var initialize = await client.PostAsync($"/api/payments/events/{eventId}/initialize", null);
        var reference = (await ReadJsonAsync(initialize)).GetProperty("reference").GetString()!;
        using var webhook = Fixture.CreateClient();
        webhook.DefaultRequestHeaders.Add("verif-hash", "valid-flutterwave-signature");

        using var response = await webhook.PostAsJsonAsync(
            "/api/payments/webhooks/flutterwave",
            new { @event = "charge.completed", data = new { status = "successful", tx_ref = reference } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await Fixture.CountRegistrationsAsync(eventId));
        using var status = await client.GetAsync($"/api/payments/{reference}");
        Assert.Equal("Failed", (await ReadJsonAsync(status)).GetProperty("status").GetString());
    }
}
