namespace EventManagement.Api.Models;

public sealed class VotingWebhookReceipt
{
    public required string Id { get; set; }
    public string Provider { get; set; } = "Paystack";
    public required string EventType { get; set; }
    public string? ProviderReference { get; set; }
    public required string Outcome { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
