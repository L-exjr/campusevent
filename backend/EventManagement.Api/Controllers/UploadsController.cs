using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
public sealed class UploadsController(
    IImageLifecycleService imageLifecycleService,
    IAuthRateLimitService rateLimitService,
    ILogger<UploadsController> logger) : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private const long MaxMultipartRequestBytes = MaxImageSizeBytes + 64 * 1024;

    private static readonly IReadOnlyDictionary<string, ImageFormat> AllowedFormats =
        new Dictionary<string, ImageFormat>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new("jpg", IsJpeg),
            ["image/png"] = new("png", IsPng),
            ["image/webp"] = new("webp", IsWebP)
        };

    [HttpPost("profile-image")]
    [RequestSizeLimit(MaxMultipartRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestBytes)]
    public Task<ActionResult<ImageUploadResponse>> UploadProfileImage(
        IFormFile? file,
        CancellationToken cancellationToken) =>
        UploadAsync(file, "profile-images", ImageUploadKind.Profile, cancellationToken);

    [HttpPost("event-image")]
    [Authorize(Roles = "Organizer,Admin")]
    [RequestSizeLimit(MaxMultipartRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestBytes)]
    public Task<ActionResult<ImageUploadResponse>> UploadEventImage(
        IFormFile? file,
        CancellationToken cancellationToken) =>
        UploadAsync(file, "event-images", ImageUploadKind.Event, cancellationToken);

    private async Task<ActionResult<ImageUploadResponse>> UploadAsync(
        IFormFile? file,
        string bucket,
        ImageUploadKind kind,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Choose an image file." });
        if (file.Length > MaxImageSizeBytes)
            return BadRequest(new { error = "Images must be 5 MB or smaller." });
        if (!AllowedFormats.TryGetValue(file.ContentType, out var format))
            return BadRequest(new { error = "Choose a JPG, PNG, or WebP image." });

        var ownerId = User.GetRequiredUserId();
        await rateLimitService.EnsureImageUploadAllowedAsync(ownerId, cancellationToken);

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        if (!format.SignatureMatches(header.AsSpan(0, bytesRead)))
            return BadRequest(new { error = "The file contents do not match its image type." });
        if (stream.CanSeek)
            stream.Position = 0;
        else
            return BadRequest(new { error = "The uploaded image could not be read." });

        try
        {
            var upload = await imageLifecycleService.CreatePendingAsync(
                stream,
                file.ContentType,
                bucket,
                format.Extension,
                ownerId,
                kind,
                cancellationToken);
            return Ok(new ImageUploadResponse(upload.PublicUrl));
        }
        catch (ImageStorageException exception)
        {
            logger.LogError(
                exception,
                "Authenticated image upload to {Bucket} failed with {FailureKind}.",
                bucket,
                exception.Kind);
            var (statusCode, message) = exception.Kind switch
            {
                ImageStorageFailureKind.Configuration => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Image storage is not configured. Please contact support."),
                ImageStorageFailureKind.ProviderRejected => (
                    StatusCodes.Status502BadGateway,
                    "Image storage rejected the upload. Please contact support."),
                _ => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Image storage is temporarily unavailable. Please try again.")
            };
            return StatusCode(
                statusCode,
                new { error = message });
        }
    }

    private static bool IsJpeg(ReadOnlySpan<byte> header) =>
        header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> header) =>
        header.Length >= 8 && header[..8].SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    private static bool IsWebP(ReadOnlySpan<byte> header) =>
        header.Length >= 12 &&
        header[..4].SequenceEqual("RIFF"u8) &&
        header.Slice(8, 4).SequenceEqual("WEBP"u8);

    private sealed record ImageFormat(
        string Extension,
        Func<ReadOnlySpan<byte>, bool> SignatureMatches);
}

public sealed record ImageUploadResponse(string Url);
