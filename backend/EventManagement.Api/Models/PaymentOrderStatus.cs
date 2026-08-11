namespace EventManagement.Api.Models;

public enum PaymentOrderStatus
{
    Pending,
    Verified,
    Failed,
    Expired,
    RefundPending,
    Refunded,
    RefundFailed
}
