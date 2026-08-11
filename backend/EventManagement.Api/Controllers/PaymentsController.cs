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
    IVotingService votingService) : ControllerBase
{
    [Authorize(Roles = "Student")]
    [HttpPost("events/{eventId:guid}/initialize")]
    public async Task<ActionResult<PaymentInitializationResponse>> Initialize(
        Guid eventId,
        CancellationToken cancellationToken) =>
        Ok(await paymentService.InitializeAsync(
            eventId,
            User.GetRequiredUserId(),
            cancellationToken));

    [Authorize(Roles = "Student")]
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
                await votingService.ProcessPaystackWebhookAsync(payload, signature, cancellationToken);
            else
                await paymentService.ProcessPaystackWebhookAsync(payload, signature, cancellationToken);
        }
        catch (PaymentProviderException)
        {
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                "Payment verification is temporarily unavailable.");
        }
        return Ok();
    }

    private static bool IsVotingReference(string payload)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("reference", out var value) &&
                value.GetString()?.StartsWith("vote_", StringComparison.Ordinal) == true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
