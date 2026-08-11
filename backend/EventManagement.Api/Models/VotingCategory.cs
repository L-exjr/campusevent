namespace EventManagement.Api.Models;

public sealed class VotingCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public VotingMode Mode { get; set; }
    public long PricePerVoteMinor { get; set; }
    public string Currency { get; set; } = "GHS";
    public int Position { get; set; }

    public VotingCampaign Campaign { get; set; } = null!;
    public ICollection<VotingNominee> Nominees { get; set; } = [];
    public ICollection<VoteRecord> Votes { get; set; } = [];
    public ICollection<VotingPaymentOrder> PaymentOrders { get; set; } = [];
}
