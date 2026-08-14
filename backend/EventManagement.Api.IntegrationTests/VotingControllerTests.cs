using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class VotingControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Free_vote_is_limited_to_one_per_student_and_results_hide_until_close()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("vote-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("free-voter@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Free voting event", 10);
        var campaign = await CreateCampaignAsync(organizer.Token, eventId, paid: false);
        var categoryId = campaign.CategoryId;
        using var client = CreateAuthenticatedClient(student.Token);

        using var first = await client.PostAsJsonAsync(
            $"/api/voting/categories/{categoryId}/votes",
            new { nomineeId = campaign.FirstNomineeId });
        using var duplicate = await client.PostAsJsonAsync(
            $"/api/voting/categories/{categoryId}/votes",
            new { nomineeId = campaign.SecondNomineeId });
        using var publicClient = Fixture.CreateClient();
        using var beforeClose = await publicClient.GetAsync($"/api/events/{eventId}/voting");

        first.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await ReadJsonAsync(beforeClose))
            .GetProperty("categories")[0].GetProperty("nominees")[0]
            .GetProperty("voteCount").ValueKind);

        await Fixture.SetVotingCampaignDatesAsync(
            eventId, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1));
        using var afterClose = await publicClient.GetAsync($"/api/events/{eventId}/voting");
        afterClose.EnsureSuccessStatusCode();
        Assert.Equal(1, (await ReadJsonAsync(afterClose))
            .GetProperty("categories")[0].GetProperty("nominees")[0]
            .GetProperty("voteCount").GetInt64());
    }

    [Fact]
    public async Task Paid_votes_are_recorded_only_after_verified_idempotent_webhook()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("paid-vote-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("paid-voter@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Paid voting event", 10);
        var campaign = await CreateCampaignAsync(organizer.Token, eventId, paid: true);
        using var client = CreateAuthenticatedClient(student.Token);

        using var initialize = await client.PostAsJsonAsync(
            $"/api/voting/categories/{campaign.CategoryId}/payments/initialize",
            new { nomineeId = campaign.FirstNomineeId, quantity = 7 });
        initialize.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync(initialize);
        var reference = body.GetProperty("reference").GetString()!;
        Assert.StartsWith("vote_", reference);
        Assert.Equal(17_500, body.GetProperty("amountMinor").GetInt64());
        Assert.Equal(0, await Fixture.GetNomineeVoteCountAsync(campaign.FirstNomineeId));

        using var webhook = Fixture.CreateClient();
        webhook.DefaultRequestHeaders.Add("x-paystack-signature", "valid-test-signature");
        using var first = await webhook.PostAsJsonAsync(
            "/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });
        using var duplicate = await webhook.PostAsJsonAsync(
            "/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });

        first.EnsureSuccessStatusCode();
        duplicate.EnsureSuccessStatusCode();
        Assert.Equal(7, await Fixture.GetNomineeVoteCountAsync(campaign.FirstNomineeId));
        using var status = await client.GetAsync($"/api/voting/payments/{reference}");
        status.EnsureSuccessStatusCode();
        var statusBody = await ReadJsonAsync(status);
        Assert.Equal("Verified", statusBody.GetProperty("status").GetString());
        Assert.True(statusBody.GetProperty("voteRecorded").GetBoolean());
    }

    [Fact]
    public async Task Paid_vote_created_before_deadline_is_honored_after_deadline_while_unexpired()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("deadline-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("deadline-voter@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Deadline vote event", 10);
        var campaign = await CreateCampaignAsync(organizer.Token, eventId, paid: true);
        using var client = CreateAuthenticatedClient(student.Token);
        using var initialize = await client.PostAsJsonAsync(
            $"/api/voting/categories/{campaign.CategoryId}/payments/initialize",
            new { nomineeId = campaign.FirstNomineeId, quantity = 2 });
        var reference = (await ReadJsonAsync(initialize)).GetProperty("reference").GetString()!;
        await Fixture.SetVotingOrderCreatedAtAsync(reference, DateTimeOffset.UtcNow.AddMinutes(-2));
        await Fixture.SetVotingCampaignDatesAsync(eventId, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddSeconds(-1));

        using var webhook = Fixture.CreateClient();
        webhook.DefaultRequestHeaders.Add("x-paystack-signature", "valid-test-signature");
        using var response = await webhook.PostAsJsonAsync("/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });

        response.EnsureSuccessStatusCode();
        Assert.Equal(2, await Fixture.GetNomineeVoteCountAsync(campaign.FirstNomineeId));
    }

    [Fact]
    public async Task Expired_paid_vote_order_is_rejected_even_when_provider_verifies_it()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("expired-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("expired-voter@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Expired vote event", 10);
        var campaign = await CreateCampaignAsync(organizer.Token, eventId, paid: true);
        using var client = CreateAuthenticatedClient(student.Token);
        using var initialize = await client.PostAsJsonAsync(
            $"/api/voting/categories/{campaign.CategoryId}/payments/initialize",
            new { nomineeId = campaign.FirstNomineeId, quantity = 2 });
        var reference = (await ReadJsonAsync(initialize)).GetProperty("reference").GetString()!;
        await Fixture.SetVotingOrderExpiryAsync(reference, DateTimeOffset.UtcNow.AddSeconds(-1));
        using var webhook = Fixture.CreateClient();
        webhook.DefaultRequestHeaders.Add("x-paystack-signature", "valid-test-signature");

        using var response = await webhook.PostAsJsonAsync("/api/payments/webhooks/paystack",
            new { @event = "charge.success", data = new { reference } });

        response.EnsureSuccessStatusCode();
        Assert.Equal(0, await Fixture.GetNomineeVoteCountAsync(campaign.FirstNomineeId));
        using var status = await client.GetAsync($"/api/voting/payments/{reference}");
        Assert.Equal("Expired", (await ReadJsonAsync(status)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Organizer_cannot_manage_another_organizers_campaign()
    {
        await ResetAsync();
        var owner = await CreateActorAsync("campaign-owner@example.test", "Organizer");
        var other = await CreateActorAsync("campaign-other@example.test", "Organizer");
        var eventId = await CreateEventAsync(owner.Token, "Protected voting event", 10);
        using var client = CreateAuthenticatedClient(other.Token);

        using var response = await client.PutAsJsonAsync(
            $"/api/events/{eventId}/voting",
            CampaignPayload(paid: false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<CampaignIds> CreateCampaignAsync(string token, Guid eventId, bool paid)
    {
        using var client = CreateAuthenticatedClient(token);
        using var response = await client.PutAsJsonAsync(
            $"/api/events/{eventId}/voting", CampaignPayload(paid));
        response.EnsureSuccessStatusCode();
        var category = (await ReadJsonAsync(response)).GetProperty("categories")[0];
        return new CampaignIds(
            category.GetProperty("id").GetGuid(),
            category.GetProperty("nominees")[0].GetProperty("id").GetGuid(),
            category.GetProperty("nominees")[1].GetProperty("id").GetGuid());
    }

    private static object CampaignPayload(bool paid) => new
    {
        opensAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        closesAt = DateTimeOffset.UtcNow.AddDays(1),
        isPublished = true,
        categories = new[]
        {
            new
            {
                name = paid ? "Paid category" : "Free category",
                description = "Choose one nominee.",
                mode = paid ? "Paid" : "Free",
                pricePerVoteMinor = paid ? 2_500 : 0,
                nominees = new[]
                {
                    new { name = "Nominee One", description = "First nominee" },
                    new { name = "Nominee Two", description = "Second nominee" }
                }
            }
        }
    };

    private sealed record CampaignIds(Guid CategoryId, Guid FirstNomineeId, Guid SecondNomineeId);
}
