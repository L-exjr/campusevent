namespace EventManagement.Api.Models;

public sealed class TicketTier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public long PriceMinor { get; set; }
    public int Capacity { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;

    public EventEntity Event { get; set; } = null!;
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = [];
}
