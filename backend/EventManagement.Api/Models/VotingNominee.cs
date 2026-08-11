namespace EventManagement.Api.Models;

public sealed class VotingNominee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int Position { get; set; }

    public VotingCategory Category { get; set; } = null!;
    public ICollection<VoteRecord> Votes { get; set; } = [];
    public ICollection<VotingPaymentOrder> PaymentOrders { get; set; } = [];
}
