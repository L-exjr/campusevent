using System.Net;

namespace EventManagement.Api.IntegrationTests;

public sealed class CertificatesControllerTests(ApiIntegrationFixture fixture)
    : IntegrationTestBase(fixture), IClassFixture<ApiIntegrationFixture>
{
    [Fact]
    public async Task Attended_student_can_generate_certificate_after_event_only_once()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("certificate-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("certificate-student@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Certificate event", 10);
        var registrationId = await RegisterForEventAsync(student.Token, eventId);
        await Fixture.SetRegistrationAttendanceAsync(registrationId, true);
        await Fixture.SetEventDateAsync(eventId, DateTimeOffset.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(student.Token);

        using var first = await client.PostAsync(
            $"/api/certificates/registrations/{registrationId}", null);
        using var second = await client.PostAsync(
            $"/api/certificates/registrations/{registrationId}", null);

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();
        var firstBody = await ReadJsonAsync(first);
        var secondBody = await ReadJsonAsync(second);
        Assert.Equal(firstBody.GetProperty("generatedAt").GetDateTimeOffset(),
            secondBody.GetProperty("generatedAt").GetDateTimeOffset());
        Assert.StartsWith("https://storage.example.test/signed/",
            firstBody.GetProperty("downloadUrl").GetString());
        var state = await Fixture.GetCertificateStateAsync(registrationId);
        Assert.Equal(1, state.TemplateVersion);
        Assert.NotNull(state.GeneratedAt);
        Assert.EndsWith("/v1.pdf", state.ObjectKey);
    }

    [Fact]
    public async Task Certificate_requires_confirmed_attendance()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("certificate-unattended-owner@example.test", "Organizer");
        var student = await RegisterStudentAsync("certificate-unattended@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Attendance required", 10);
        var registrationId = await RegisterForEventAsync(student.Token, eventId);
        await Fixture.SetEventDateAsync(eventId, DateTimeOffset.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(student.Token);

        using var response = await client.PostAsync(
            $"/api/certificates/registrations/{registrationId}", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Student_cannot_download_another_students_certificate()
    {
        await ResetAsync();
        var organizer = await CreateActorAsync("certificate-private-owner@example.test", "Organizer");
        var owner = await RegisterStudentAsync("certificate-real-student@example.test");
        var other = await RegisterStudentAsync("certificate-other-student@example.test");
        var eventId = await CreateEventAsync(organizer.Token, "Private certificate", 10);
        var registrationId = await RegisterForEventAsync(owner.Token, eventId);
        await Fixture.SetRegistrationAttendanceAsync(registrationId, true);
        await Fixture.SetEventDateAsync(eventId, DateTimeOffset.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(other.Token);

        using var response = await client.PostAsync(
            $"/api/certificates/registrations/{registrationId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
