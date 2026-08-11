using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Payments;

public sealed record PaymentInitializationResponse(
    string Reference,
    string AuthorizationUrl,
    long AmountMinor,
    string Currency,
    DateTimeOffset ExpiresAt);

public sealed record PaymentStatusResponse(
    string Reference,
    PaymentOrderStatus Status,
    long AmountMinor,
    string Currency,
    Guid? RegistrationId,
    DateTimeOffset ExpiresAt);
