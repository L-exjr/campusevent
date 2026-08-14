namespace EventManagement.Api.Models;

public sealed class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizerId { get; set; }
    public Guid? EventId { get; set; }
    public required string Code { get; set; }
    public int PercentageDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Organizer { get; set; } = null!;
    public EventEntity? Event { get; set; }
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = [];
}
