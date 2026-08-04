using System.Net;
using EventManagement.Api.Models;
using EventManagement.Api.Services;

namespace EventManagement.Api.IntegrationTests;

public sealed class EmailOutboxControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Admin_can_list_and_retry_a_safe_failed_message()
    {
        await ResetAsync();
        var messageId = await Fixture.CreateFailedEmailOutboxMessageAsync(
            EmailOutbox.OrganizerApplicationDecisionKind,
            "{}");
        var admin = await LoginAdminAsync();
        using var client = CreateAuthenticatedClient(admin.Token);

        using var list = await client.GetAsync("/api/email-outbox/failed?page=1&pageSize=20");
        list.EnsureSuccessStatusCode();
        var item = (await ReadJsonAsync(list)).GetProperty("items").EnumerateArray().Single();
        Assert.Equal(messageId, item.GetProperty("id").GetGuid());
        Assert.True(item.GetProperty("canRetry").GetBoolean());

        using var retry = await client.PutAsync($"/api/email-outbox/{messageId}/retry", null);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        var state = await Fixture.GetEmailOutboxStateAsync(messageId);
        Assert.Equal(EmailOutboxStatus.Pending, state.Status);
        Assert.Equal(0, state.AttemptCount);
    }

    [Fact]
    public async Task Password_reset_dead_letter_cannot_be_retried()
    {
        await ResetAsync();
        var messageId = await Fixture.CreateFailedEmailOutboxMessageAsync(
            EmailOutbox.PasswordResetKind,
            payloadJson: null);
        var admin = await LoginAdminAsync();
        using var client = CreateAuthenticatedClient(admin.Token);

        using var retry = await client.PutAsync($"/api/email-outbox/{messageId}/retry", null);

        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal(EmailOutboxStatus.Failed, (await Fixture.GetEmailOutboxStateAsync(messageId)).Status);
    }

    [Fact]
    public async Task Non_admin_cannot_view_dead_letters()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("dead-letter-student@example.test");
        using var client = CreateAuthenticatedClient(student.Token);

        using var response = await client.GetAsync("/api/email-outbox/failed");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
