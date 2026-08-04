using System.Net;
using System.Net.Http.Json;

namespace EventManagement.Api.IntegrationTests;

public sealed class UsersControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Authenticated_user_can_update_own_profile_image_URL()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("profile-owner@example.test");
        using var client = CreateAuthenticatedClient(student.Token);
        using var upload = await client.PostAsync("/api/uploads/profile-image", CreatePngUpload());
        upload.EnsureSuccessStatusCode();
        var imageUrl = (await ReadJsonAsync(upload)).GetProperty("url").GetString()!;

        using var response = await client.PutAsJsonAsync(
            $"/api/users/{student.UserId}/profile",
            new { imageUrl });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(imageUrl, (await ReadJsonAsync(response)).GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task Authenticated_user_cannot_update_another_users_profile()
    {
        await ResetAsync();
        var actor = await RegisterStudentAsync("profile-actor@example.test");
        var target = await RegisterStudentAsync("profile-target@example.test");
        using var client = CreateAuthenticatedClient(actor.Token);

        using var response = await client.PutAsJsonAsync(
            $"/api/users/{target.UserId}/profile",
            new { imageUrl = "https://project.supabase.co/profile.jpg" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
