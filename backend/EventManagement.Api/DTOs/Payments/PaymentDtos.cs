using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Payments;

public sealed record PaymentInitializationRequest(Guid? TicketTierId, string? CouponCode);

public sealed record PaymentInitializationResponse(
    string Reference,
    string AuthorizationUrl,
    long AmountMinor,
    long OriginalAmountMinor,
    long DiscountAmountMinor,
    Guid? TicketTierId,
    string? TicketTierName,
    string? CouponCode,
    string Currency,
    DateTimeOffset ExpiresAt);

public sealed record PaymentStatusResponse(
    string Reference,
    PaymentOrderStatus Status,
    long AmountMinor,
    string Currency,
    Guid? RegistrationId,
    DateTimeOffset ExpiresAt);
