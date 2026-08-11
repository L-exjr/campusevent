using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Certificates;
using EventManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface ICertificateService
{
    Task<CertificateDownloadResponse> GetOrCreateAsync(
        Guid registrationId,
        Guid studentId,
        CancellationToken cancellationToken);
}

public sealed class CertificateService(
    AppDbContext dbContext,
    ICertificatePdfGenerator pdfGenerator,
    ICertificateStorageService storageService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    TimeProvider timeProvider) : ICertificateService
{
    public async Task<CertificateDownloadResponse> GetOrCreateAsync(
        Guid registrationId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var registration = await dbContext.EventRegistrations
            .FromSqlInterpolated(
                $"SELECT * FROM \"EventRegistrations\" WHERE \"Id\" = {registrationId} FOR UPDATE")
            .Include(item => item.Event)
                .ThenInclude(eventEntity => eventEntity.Organizer)
            .Include(item => item.Student)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Registration not found.");

        if (registration.StudentId != studentId)
            throw new ApiException(StatusCodes.Status403Forbidden,
                "Students may only download their own certificates.");
        if (!registration.Attended)
            throw new ApiException(StatusCodes.Status409Conflict,
                "A certificate is available only after attendance has been confirmed.");
        var now = timeProvider.GetUtcNow();
        if (registration.Event.Date > now)
            throw new ApiException(StatusCodes.Status409Conflict,
                "A certificate is available only after the event has ended.");

        if (string.IsNullOrWhiteSpace(registration.CertificateObjectKey))
        {
            var templateVersion = Math.Max(1, configuration.GetValue("Certificates:TemplateVersion", 1));
            var objectKey =
                $"events/{registration.EventId:N}/registrations/{registration.Id:N}/v{templateVersion}.pdf";
            var logo = await TryDownloadTrustedLogoAsync(registration.Event.ImageUrl, cancellationToken);
            var pdf = pdfGenerator.Generate(new CertificatePdfModel(
                registration.Student.Name,
                registration.Event.Title,
                registration.Event.Date,
                registration.Event.Organizer.Name,
                registration.Id,
                logo));
            await storageService.UploadAsync(objectKey, pdf, cancellationToken);
            registration.CertificateObjectKey = objectKey;
            registration.CertificateGeneratedAt = now;
            registration.CertificateTemplateVersion = templateVersion;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var signedUrl = await storageService.CreateSignedUrlAsync(
            registration.CertificateObjectKey!, cancellationToken);
        return new CertificateDownloadResponse(
            registration.Id,
            signedUrl.Url,
            signedUrl.ExpiresAt,
            registration.CertificateGeneratedAt ?? now);
    }

    private async Task<byte[]?> TryDownloadTrustedLogoAsync(
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var logoUri)) return null;
        var supabaseUrl = configuration["SUPABASE_URL"];
        if (string.IsNullOrWhiteSpace(supabaseUrl)) supabaseUrl = configuration["Supabase:Url"];
        if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var supabaseUri) ||
            !logoUri.Host.Equals(supabaseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            !logoUri.AbsolutePath.StartsWith("/storage/v1/object/public/", StringComparison.Ordinal))
            return null;

        using var response = await httpClientFactory.CreateClient().GetAsync(
            logoUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 5 * 1024 * 1024)
            return null;
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not ("image/png" or "image/jpeg")) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (output.Length + read > 5 * 1024 * 1024) return null;
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return output.ToArray();
    }
}
