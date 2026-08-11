namespace EventManagement.Api.Models;

public sealed class VotingCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public DateTimeOffset OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public EventEntity Event { get; set; } = null!;
    public ICollection<VotingCategory> Categories { get; set; } = [];
}
