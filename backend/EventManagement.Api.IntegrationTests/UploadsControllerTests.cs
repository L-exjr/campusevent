using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EventManagement.Api.Models;

namespace EventManagement.Api.IntegrationTests;

public sealed class UploadsControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Anonymous_profile_upload_is_rejected_before_file_processing()
    {
        await ResetAsync();
        using var client = CookieClient();
        using var csrfResponse = await client.GetAsync("/api/auth/csrf");
        csrfResponse.EnsureSuccessStatusCode();
        var csrf = (await ReadJsonAsync(csrfResponse)).GetProperty("token").GetString()!;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/uploads/profile-image") {
            Content = CreatePngUpload()
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Student_cannot_upload_event_cover()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("upload-student@example.test");
        using var client = CreateAuthenticatedClient(student.Token);
        using var response = await client.PostAsync(
            "/api/uploads/event-image",
            CreatePngUpload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Event_owner_can_upload_cover_for_owned_event()
    {
        await ResetAsync();
        var owner = await RegisterStudentAsync("upload-owner@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Owned upload event", 10);
        using var client = CreateAuthenticatedClient(owner.Token);

        using var response = await client.PostAsync(
            $"/api/uploads/event-image?eventId={eventId}",
            CreatePngUpload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_non_owner_cannot_upload_cover_for_another_event()
    {
        await ResetAsync();
        var owner = await RegisterStudentAsync("upload-event-owner@example.test");
        var attacker = await RegisterStudentAsync("upload-non-owner@example.test");
        var eventId = await CreateEventAsync(owner.Token, "Protected upload event", 10);
        using var client = CreateAuthenticatedClient(attacker.Token);

        using var response = await client.PostAsync(
            $"/api/uploads/event-image?eventId={eventId}",
            CreatePngUpload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_upload_event_cover_without_ownership()
    {
        await ResetAsync();
        var owner = await RegisterStudentAsync("upload-admin-target@example.test");
        var admin = await LoginAdminAsync();
        var eventId = await CreateEventAsync(owner.Token, "Admin upload event", 10);
        using var client = CreateAuthenticatedClient(admin.Token);

        using var response = await client.PostAsync(
            $"/api/uploads/event-image?eventId={eventId}",
            CreatePngUpload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Profile_upload_is_owner_scoped_claimed_and_superseded_atomically()
    {
        await ResetAsync();
        var student = await RegisterStudentAsync("profile-image-owner@example.test");
        using var client = CreateAuthenticatedClient(student.Token);

        using var firstUploadResponse = await client.PostAsync(
            "/api/uploads/profile-image",
            CreatePngUpload());
        firstUploadResponse.EnsureSuccessStatusCode();
        var firstUrl = (await ReadJsonAsync(firstUploadResponse)).GetProperty("url").GetString()!;
        using var firstUpdate = await client.PutAsJsonAsync(
            $"/api/users/{student.UserId}/profile",
            new { imageUrl = firstUrl });
        firstUpdate.EnsureSuccessStatusCode();
        var firstState = await Fixture.GetUserImageStateAsync(student.UserId);
        Assert.StartsWith($"{student.UserId:N}/", firstState.ObjectKey);
        Assert.Equal(ImageUploadStatus.Claimed, await Fixture.GetImageUploadStatusAsync(firstState.ObjectKey!));

        using var secondUploadResponse = await client.PostAsync(
            "/api/uploads/profile-image",
            CreatePngUpload());
        secondUploadResponse.EnsureSuccessStatusCode();
        var secondUrl = (await ReadJsonAsync(secondUploadResponse)).GetProperty("url").GetString()!;
        using var secondUpdate = await client.PutAsJsonAsync(
            $"/api/users/{student.UserId}/profile",
            new { imageUrl = secondUrl });
        secondUpdate.EnsureSuccessStatusCode();

        var secondState = await Fixture.GetUserImageStateAsync(student.UserId);
        Assert.NotEqual(firstState.ObjectKey, secondState.ObjectKey);
        Assert.Equal(ImageUploadStatus.DeletePending, await Fixture.GetImageUploadStatusAsync(firstState.ObjectKey!));
        Assert.Equal(ImageUploadStatus.Claimed, await Fixture.GetImageUploadStatusAsync(secondState.ObjectKey!));
    }

}
