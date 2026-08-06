using System.Net;

namespace EventManagement.Api.IntegrationTests;

public sealed class AdminAuditLogsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Admin_mutation_is_recorded_and_visible_to_admins()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("audit-role-change@example.test");
        var admin = await LoginAdminAsync();
        await SetRoleAsync(admin.Token, student.UserId, "Organizer");
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.GetAsync("/api/admin-audit-logs?search=UserRoleChanged");

        response.EnsureSuccessStatusCode();
        var item = (await ReadJsonAsync(response)).GetProperty("items").EnumerateArray().Single();
        Assert.Equal(admin.UserId, item.GetProperty("actorUserId").GetGuid());
        Assert.Equal("UserRoleChanged", item.GetProperty("action").GetString());
        Assert.Equal("User", item.GetProperty("targetType").GetString());
        Assert.Equal(student.UserId.ToString(), item.GetProperty("targetId").GetString());
        Assert.True(await Fixture.AdminAuditMutationIsRejectedAsync(
            item.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Non_admin_cannot_read_audit_log()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("audit-forbidden@example.test");
        using var client = CreateAuthenticatedClient(student.Token);

        using var response = await client.GetAsync("/api/admin-audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_export_a_bounded_csv_audit_report()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("audit-export@example.test");
        var admin = await LoginAdminAsync();
        await SetRoleAsync(admin.Token, student.UserId, "Organizer");
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.GetAsync("/api/admin-audit-logs/export");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("UserRoleChanged", csv);
        Assert.Contains("actorUserId", csv);
    }
}
