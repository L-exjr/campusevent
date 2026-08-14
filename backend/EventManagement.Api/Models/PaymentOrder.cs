namespace EventManagement.Api.Models;

public sealed class PaymentOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? TicketTierId { get; set; }
    public Guid? CouponId { get; set; }
    public long OriginalAmountMinor { get; set; }
    public long DiscountAmountMinor { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "GHS";
    public string Provider { get; set; } = "Paystack";
    public required string ProviderReference { get; set; }
    public string? AuthorizationUrl { get; set; }
    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public EventEntity Event { get; set; } = null!;
    public User Student { get; set; } = null!;
    public EventRegistration? Registration { get; set; }
    public TicketTier? TicketTier { get; set; }
    public Coupon? Coupon { get; set; }
}
