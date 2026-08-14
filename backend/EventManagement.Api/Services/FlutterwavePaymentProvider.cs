using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventManagement.Api.Services;

public sealed class FlutterwavePaymentProvider(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<FlutterwavePaymentProvider> logger) : IPaymentProvider
{
    private const string DefaultBaseUrl = "https://api.flutterwave.com/v3";
    public string Name => "Flutterwave";

    public bool HasValidSignature(string payload, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        var expected = Encoding.UTF8.GetBytes(GetWebhookSecret());
        var supplied = Encoding.UTF8.GetBytes(signature);
        return expected.Length == supplied.Length && CryptographicOperations.FixedTimeEquals(expected, supplied);
    }

    public bool TryGetSuccessfulWebhook(string payload, out PaymentWebhookNotification? notification)
    {
        notification = null;
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("event", out var eventValue) ||
            !string.Equals(eventValue.GetString(), "charge.completed", StringComparison.Ordinal) ||
            !root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("status", out var statusValue) ||
            !string.Equals(statusValue.GetString(), "successful", StringComparison.OrdinalIgnoreCase) ||
            !data.TryGetProperty("tx_ref", out var referenceValue) ||
            string.IsNullOrWhiteSpace(referenceValue.GetString())) return false;
        notification = new PaymentWebhookNotification("charge.completed", referenceValue.GetString()!);
        return true;
    }

    public async Task<PaymentProviderInitialization> InitializeAsync(string email, long amountMinor,
        string currency, string reference, string callbackUrl, Guid orderId, Guid eventId,
        Guid studentId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/payments");
        request.Content = JsonContent.Create(new
        {
            tx_ref = reference,
            amount = ToMajorUnits(amountMinor),
            currency,
            redirect_url = callbackUrl,
            customer = new { email },
            customizations = new { title = "Campus Events", description = "Event payment" },
            meta = new { payment_order_id = orderId, event_id = eventId, student_id = studentId }
        });
        using var response = await SendAsync(request, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<InitializeData>>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || envelope?.Status != "success" || string.IsNullOrWhiteSpace(envelope.Data?.Link))
        {
            logger.LogWarning("Flutterwave rejected payment initialization for reference {Reference} with status {StatusCode}.", reference, (int)response.StatusCode);
            throw new PaymentProviderException("Flutterwave could not initialize the payment.");
        }
        return new PaymentProviderInitialization(envelope.Data.Link, reference);
    }

    public async Task<PaymentProviderVerification> VerifyAsync(string reference, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/transactions/verify_by_reference?tx_ref={Uri.EscapeDataString(reference)}");
        using var response = await SendAsync(request, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<VerifyData>>(cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || envelope?.Status != "success" || envelope.Data is null)
            throw new PaymentProviderException("Flutterwave could not verify the payment.");
        return new PaymentProviderVerification(
            string.Equals(envelope.Data.Status, "successful", StringComparison.OrdinalIgnoreCase),
            envelope.Data.TxRef,
            checked((long)Math.Round(envelope.Data.Amount * 100m, MidpointRounding.AwayFromZero)),
            envelope.Data.Currency.ToUpperInvariant());
    }

    public async Task<bool> RequestRefundAsync(string reference, long amountMinor, CancellationToken cancellationToken)
    {
        var transaction = await GetTransactionAsync(reference, cancellationToken);
        if (transaction is null) return false;
        using var request = CreateRequest(HttpMethod.Post, $"/transactions/{transaction.Id}/refund");
        request.Content = JsonContent.Create(new { amount = ToMajorUnits(amountMinor) });
        using var response = await SendAsync(request, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<JsonElement>>(cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode && envelope?.Status == "success";
    }

    private async Task<VerifyData?> GetTransactionAsync(string reference, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, $"/transactions/verify_by_reference?tx_ref={Uri.EscapeDataString(reference)}");
        using var response = await SendAsync(request, cancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<VerifyData>>(cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode && envelope?.Status == "success" ? envelope.Data : null;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var baseUrl = configuration["Payments:Flutterwave:BaseUrl"]?.TrimEnd('/') ?? DefaultBaseUrl;
        var request = new HttpRequestMessage(method, baseUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetSecret());
        return request;
    }

    private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        httpClientFactory.CreateClient(nameof(FlutterwavePaymentProvider)).SendAsync(request, cancellationToken);

    private string GetSecret() => GetRequired("FLUTTERWAVE_SECRET_KEY", "Payments:Flutterwave:SecretKey", "Flutterwave secret key");
    private string GetWebhookSecret() => GetRequired("FLUTTERWAVE_WEBHOOK_SECRET", "Payments:Flutterwave:WebhookSecret", "Flutterwave webhook secret");
    private string GetRequired(string environmentKey, string configKey, string label)
    {
        var value = configuration[environmentKey] ?? configuration[configKey];
        if (string.IsNullOrWhiteSpace(value)) throw new PaymentProviderException($"{label} is not configured.");
        return value;
    }
    private static decimal ToMajorUnits(long amountMinor) => amountMinor / 100m;
    private sealed record Envelope<T>(string Status, string Message, T? Data);
    private sealed record InitializeData(string Link);
    private sealed record VerifyData(long Id, string Status, [property: JsonPropertyName("tx_ref")] string TxRef, decimal Amount, string Currency);
}
