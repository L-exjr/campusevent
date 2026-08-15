using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Tickets;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface ITicketService
{
    Task<TicketResponse> GetAsync(
        Guid registrationId,
        Guid studentId,
        CancellationToken cancellationToken);
    Task<CheckInResponse> CheckInAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        string token,
        CancellationToken cancellationToken);
    Task<CheckInResponse> CheckInByCodeAsync(
        Guid eventId, Guid actorId, UserRole actorRole, string ticketCode,
        CancellationToken cancellationToken);
}

public sealed class TicketService(
    AppDbContext dbContext,
    ITicketTokenService tokenService,
    IEventAuthorizationService authorizationService,
    TimeProvider timeProvider) : ITicketService
{
    public async Task<TicketResponse> GetAsync(
        Guid registrationId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var registration = await dbContext.EventRegistrations.AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.Student)
            .SingleOrDefaultAsync(item => item.Id == registrationId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Registration not found.");
        if (registration.StudentId != studentId)
            throw new ApiException(StatusCodes.Status403Forbidden, "Students may only view their own tickets.");
        var expiresAt = registration.Event.Date.AddDays(1);
        if (expiresAt <= timeProvider.GetUtcNow())
            throw new ApiException(StatusCodes.Status410Gone, "This ticket has expired.");
        return new TicketResponse(
            registration.Id,
            registration.EventId,
            registration.Event.Title,
            registration.Student.Name,
            registration.TicketCode,
            tokenService.Create(
                registration.Id,
                registration.EventId,
                registration.StudentId,
                expiresAt),
            expiresAt);
    }

    public async Task<CheckInResponse> CheckInAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        string token,
        CancellationToken cancellationToken)
    {
        var claims = tokenService.Validate(token);
        if (claims.EventId != eventId)
            throw new ApiException(StatusCodes.Status400BadRequest, "This ticket belongs to a different event.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var eventEntity = await dbContext.Events.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == eventId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.CheckIn, cancellationToken);

        var registration = await dbContext.EventRegistrations
            .FromSqlInterpolated(
                $"SELECT * FROM \"EventRegistrations\" WHERE \"Id\" = {claims.RegistrationId} FOR UPDATE")
            .Include(item => item.Student)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Registration not found.");
        if (registration.EventId != eventId || registration.StudentId != claims.StudentId)
            throw new ApiException(StatusCodes.Status400BadRequest, "The ticket does not match this registration.");
        if (registration.Attended)
            throw new ApiException(StatusCodes.Status409Conflict, "This ticket has already been checked in.");

        registration.Attended = true;
        var checkedInAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CheckInResponse(
            registration.Id,
            registration.EventId,
            registration.Student.Name,
            checkedInAt);
    }

    public async Task<CheckInResponse> CheckInByCodeAsync(
        Guid eventId, Guid actorId, UserRole actorRole, string ticketCode,
        CancellationToken cancellationToken)
    {
        var normalized = ticketCode.Trim().ToUpperInvariant();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var eventEntity = await dbContext.Events.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == eventId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.CheckIn, cancellationToken);
        var registration = await dbContext.EventRegistrations
            .FromSqlInterpolated(
                $"SELECT * FROM \"EventRegistrations\" WHERE \"TicketCode\" = {normalized} FOR UPDATE")
            .Include(item => item.Student)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Ticket code was not found.");
        if (registration.EventId != eventId)
            throw new ApiException(StatusCodes.Status404NotFound, "Ticket code was not found for this event.");
        if (registration.Attended)
            throw new ApiException(StatusCodes.Status409Conflict, "This ticket has already been checked in.");
        registration.Attended = true;
        var checkedInAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CheckInResponse(registration.Id, eventId, registration.Student.Name, checkedInAt);
    }
}
