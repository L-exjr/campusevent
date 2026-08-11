namespace EventManagement.Api.Models;

public sealed class VotingPaymentOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public Guid NomineeId { get; set; }
    public Guid VoterId { get; set; }
    public int Quantity { get; set; }
    public long UnitPriceMinor { get; set; }
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

    public VotingCategory Category { get; set; } = null!;
    public VotingNominee Nominee { get; set; } = null!;
    public User Voter { get; set; } = null!;
    public VoteRecord? Vote { get; set; }
}
