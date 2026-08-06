using System.Net;
using EventManagement.Api.Models;

namespace EventManagement.Api.IntegrationTests;

public sealed class ImageCleanupControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Admin_can_list_and_retry_a_failed_image_cleanup()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("image-recovery-owner@example.test", "Organizer");
        var imageId = await Fixture.CreateFailedImageCleanupAsync(organizer.UserId);
        var admin = await LoginAdminAsync();
        using var client = CreateAuthenticatedClient(admin.Token);

        using var list = await client.GetAsync("/api/image-cleanup/failed");
        list.EnsureSuccessStatusCode();
        Assert.Contains((await ReadJsonAsync(list)).GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == imageId);

        using var retry = await client.PutAsync($"/api/image-cleanup/{imageId}/retry", null);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        var state = await Fixture.GetImageCleanupStateAsync(imageId);
        Assert.Equal(ImageUploadStatus.DeletePending, state.Status);
        Assert.Equal(0, state.Attempts);
        Assert.Equal(8, state.LifetimeAttempts);
        Assert.Equal(1, state.ManualRetries);
    }

    [Fact]
    public async Task Non_admin_cannot_read_failed_image_cleanup()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("image-recovery-forbidden@example.test");
        using var client = CreateAuthenticatedClient(student.Token);
        using var response = await client.GetAsync("/api/image-cleanup/failed");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
