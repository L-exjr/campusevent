namespace EventManagement.Api.Models;

public sealed class VoteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public Guid NomineeId { get; set; }
    public Guid VoterId { get; set; }
    public int Quantity { get; set; }
    public Guid? VotingPaymentOrderId { get; set; }
    public DateTimeOffset CastAt { get; set; } = DateTimeOffset.UtcNow;

    public VotingCategory Category { get; set; } = null!;
    public VotingNominee Nominee { get; set; } = null!;
    public User Voter { get; set; } = null!;
    public VotingPaymentOrder? VotingPaymentOrder { get; set; }
}
