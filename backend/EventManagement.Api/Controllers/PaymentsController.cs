using EventManagement.Api.DTOs.Payments;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    IPaymentService paymentService,
    IVotingService votingService,
    OperationalMetrics metrics) : ControllerBase
{
    [Authorize(Roles = "Student,Organizer")]
    [HttpPost("events/{eventId:guid}/initialize")]
    public async Task<ActionResult<PaymentInitializationResponse>> Initialize(
        Guid eventId,
        [FromQuery] Guid? ticketTierId,
        [FromQuery] string? couponCode,
        CancellationToken cancellationToken) =>
        Ok(await paymentService.InitializeAsync(
            eventId,
            User.GetRequiredUserId(),
            ticketTierId,
            couponCode,
            cancellationToken));

    [Authorize(Roles = "Student,Organizer")]
    [HttpGet("{reference}")]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(
        string reference,
        CancellationToken cancellationToken) =>
        Ok(await paymentService.GetStatusAsync(
            reference,
            User.GetRequiredUserId(),
            cancellationToken));

    [AllowAnonymous]
    [HttpPost("webhooks/paystack")]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        try
        {
            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
            if (IsVotingReference(payload))
                await votingService.ProcessWebhookAsync("Paystack", payload, signature, cancellationToken);
            else
                await paymentService.ProcessWebhookAsync("Paystack", payload, signature, cancellationToken);
        }
        catch (PaymentProviderException)
        {
            metrics.PaymentCallback(false);
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                "Payment verification is temporarily unavailable.");
        }
        catch { metrics.PaymentCallback(false); throw; }
        metrics.PaymentCallback(true);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("webhooks/flutterwave")]
    public async Task<IActionResult> FlutterwaveWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        try
        {
            var signature = Request.Headers["verif-hash"].FirstOrDefault();
            if (IsVotingReference(payload, "tx_ref"))
                await votingService.ProcessWebhookAsync("Flutterwave", payload, signature, cancellationToken);
            else
                await paymentService.ProcessWebhookAsync("Flutterwave", payload, signature, cancellationToken);
        }
        catch (PaymentProviderException)
        {
            metrics.PaymentCallback(false);
            throw new ApiException(StatusCodes.Status503ServiceUnavailable,
                "Payment verification is temporarily unavailable.");
        }
        catch { metrics.PaymentCallback(false); throw; }
        metrics.PaymentCallback(true);
        return Ok();
    }

    private static bool IsVotingReference(string payload, string referenceProperty = "reference")
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty(referenceProperty, out var value) &&
                value.GetString()?.StartsWith("vote_", StringComparison.Ordinal) == true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
