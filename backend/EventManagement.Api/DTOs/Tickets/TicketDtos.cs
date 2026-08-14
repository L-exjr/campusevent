namespace EventManagement.Api.DTOs.Tickets;

public sealed record TicketResponse(
    Guid RegistrationId,
    Guid EventId,
    string EventTitle,
    string StudentName,
    string TicketCode,
    string Token,
    DateTimeOffset ExpiresAt);

public sealed record CheckInRequest(string Token);
public sealed record ManualCheckInRequest(string TicketCode);

public sealed record CheckInResponse(
    Guid RegistrationId,
    Guid EventId,
    string StudentName,
    DateTimeOffset CheckedInAt);
