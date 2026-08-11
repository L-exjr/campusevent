namespace EventManagement.Api.DTOs.Certificates;

public sealed record CertificateDownloadResponse(
    Guid RegistrationId,
    string DownloadUrl,
    DateTimeOffset ExpiresAt,
    DateTimeOffset GeneratedAt);
