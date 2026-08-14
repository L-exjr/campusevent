using System.ComponentModel.DataAnnotations;

namespace EventManagement.Api.DTOs.Events;

public sealed record TicketTierInput(
    Guid? Id,
    [param: Required, StringLength(80, MinimumLength = 1)] string Name,
    [param: Range(0, long.MaxValue)] long PriceMinor,
    [param: Range(1, 100000)] int Capacity);

public sealed record TicketTierResponse(Guid Id, string Name, long PriceMinor, int Capacity, int Sold, bool IsActive);

public sealed record EventUpsertRequest(
    [param: Required, StringLength(200, MinimumLength = 3)] string Title,
    [param: Required, StringLength(5000, MinimumLength = 10)] string Description,
    DateTimeOffset Date,
    [param: Required, StringLength(300)] string Location,
    [param: Range(1, 100000)] int Capacity,
    [param: Required, StringLength(100)] string Category,
    [param: Url, StringLength(2048)] string? ImageUrl,
    bool? IsPublished = null,
    int? Version = null,
    [param: Range(0, long.MaxValue)] long PriceMinor = 0,
    [param: Required, StringLength(3, MinimumLength = 3)] string Currency = "GHS",
    [param: Required, StringLength(20)] string Format = "Physical",
    [param: Url, StringLength(2048)] string? MeetingUrl = null,
    DateTimeOffset? SalesStartsAt = null,
    DateTimeOffset? SalesEndsAt = null,
    DateTimeOffset? EndDate = null,
    [param: StringLength(40)] string? VirtualPlatform = null,
    double? Latitude = null,
    double? Longitude = null,
    [param: Url, StringLength(2048)] string? InstagramUrl = null,
    [param: Url, StringLength(2048)] string? TwitterUrl = null,
    [param: Url, StringLength(2048)] string? FacebookUrl = null,
    [param: Url, StringLength(2048)] string? WebsiteUrl = null,
    bool TicketingEnabled = false,
    bool RegistrationsEnabled = true,
    bool VotingEnabled = false,
    IReadOnlyList<TicketTierInput>? TicketTiers = null);

public sealed record TransferEventOwnershipRequest(
    Guid OrganizerId,
    [param: Range(1, int.MaxValue)] int Version);

public sealed record EventResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset Date,
    string Location,
    int Capacity,
    string Category,
    Guid OrganizerId,
    string OrganizerName,
    int RegistrationCount,
    DateTimeOffset CreatedAt,
    string? ImageUrl,
    bool IsPublished,
    int Version,
    long PriceMinor = 0,
    string Currency = "GHS",
    string Format = "Physical",
    string? MeetingUrl = null,
    DateTimeOffset? SalesStartsAt = null,
    DateTimeOffset? SalesEndsAt = null,
    DateTimeOffset? EndDate = null,
    string? VirtualPlatform = null,
    double? Latitude = null,
    double? Longitude = null,
    string? InstagramUrl = null,
    string? TwitterUrl = null,
    string? FacebookUrl = null,
    string? WebsiteUrl = null,
    bool TicketingEnabled = false,
    bool RegistrationsEnabled = true,
    bool VotingEnabled = false,
    IReadOnlyList<TicketTierResponse>? TicketTiers = null);

public sealed record EventRegistrantResponse(
    Guid RegistrationId,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    DateTimeOffset RegisteredAt,
    bool Attended);

public sealed record OrganizerAnalyticsPoint(DateOnly Date, int Registrations);
public sealed record OrganizerAnalyticsResponse(
    int RegistrationCount,
    long TicketRevenueMinor,
    string Currency,
    int AttendedCount,
    decimal AttendanceRate,
    IReadOnlyList<OrganizerAnalyticsPoint> RegistrationsOverTime);

public sealed record AttendanceUpdateItem(Guid RegistrationId, bool Attended);

public sealed record BulkAttendanceRequest(
    [param: Required, MinLength(1)] IReadOnlyList<AttendanceUpdateItem> Registrations);

public sealed record StudentRegistrationResponse(
    Guid RegistrationId,
    DateTimeOffset RegisteredAt,
    bool Attended,
    EventResponse Event);

public sealed record RegistrationStatusResponse(bool IsRegistered);
