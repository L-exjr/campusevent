using System.ComponentModel.DataAnnotations;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Organizers;

public sealed record PublicOrganizerSummary(
    Guid Id, string Name, string? ImageUrl, string? BannerUrl, string? Bio,
    IReadOnlyList<string> Specialties, VerificationStatus VerificationStatus);

public sealed record PublicOrganizerDetail(
    Guid Id, string Name, string? ImageUrl, string? BannerUrl, string? Bio,
    string? InstagramUrl, string? TwitterUrl, string? FacebookUrl, string? WebsiteUrl,
    IReadOnlyList<string> Specialties, VerificationStatus VerificationStatus,
    IReadOnlyList<EventResponse> Events);

public sealed record OrganizerDirectorySettings(
    bool IsVisible, string? Bio, string? BannerUrl, string? InstagramUrl,
    string? TwitterUrl, string? FacebookUrl, string? WebsiteUrl,
    IReadOnlyList<string> Specialties);

public sealed record UpdateOrganizerDirectorySettings(
    bool IsVisible,
    [param: StringLength(3000)] string? Bio,
    [param: Url, StringLength(2048)] string? BannerUrl,
    [param: Url, StringLength(2048)] string? InstagramUrl,
    [param: Url, StringLength(2048)] string? TwitterUrl,
    [param: Url, StringLength(2048)] string? FacebookUrl,
    [param: Url, StringLength(2048)] string? WebsiteUrl,
    IReadOnlyList<string> Specialties);
