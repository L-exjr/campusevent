using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventManagement.Api.Services;

public sealed record PaymentProviderInitialization(
    string AuthorizationUrl,
    string Reference);

public sealed record PaymentProviderVerification(
    bool IsSuccessful,
    string Reference,
    long AmountMinor,
    string Currency);

public sealed record PaymentWebhookNotification(string EventType, string Reference);

public interface IPaymentProvider
{
    string Name { get; }
    bool HasValidSignature(string payload, string? signature);
    bool TryGetSuccessfulWebhook(string payload, out PaymentWebhookNotification? notification);
    Task<PaymentProviderInitialization> InitializeAsync(
        string email,
        long amountMinor,
        string currency,
        string reference,
        string callbackUrl,
        Guid orderId,
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken);
    Task<PaymentProviderVerification> VerifyAsync(string reference, CancellationToken cancellationToken);
    Task<bool> RequestRefundAsync(string reference, long amountMinor, CancellationToken cancellationToken);
}

public sealed class PaystackPaymentProvider(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<PaystackPaymentProvider> logger) : IPaymentProvider
{
    private const string BaseUrl = "https://api.paystack.co";
    public string Name => "Paystack";

    public bool HasValidSignature(string payload, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        var secret = GetSecret();
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }
        return supplied.Length == expected.Length &&
            CryptographicOperations.FixedTimeEquals(supplied, expected);
    }

    public bool TryGetSuccessfulWebhook(string payload, out PaymentWebhookNotification? notification)
    {
        notification = null;
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("event", out var eventValue) ||
            !string.Equals(eventValue.GetString(), "charge.success", StringComparison.Ordinal) ||
            !root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("reference", out var referenceValue) ||
            string.IsNullOrWhiteSpace(referenceValue.GetString())) return false;
        notification = new PaymentWebhookNotification("charge.success", referenceValue.GetString()!);
        return true;
    }

    public async Task<PaymentProviderInitialization> InitializeAsync(
        string email,
        long amountMinor,
        string currency,
        string reference,
        string callbackUrl,
        Guid orderId,
        Guid eventId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/transaction/initialize");
        request.Content = JsonContent.Create(new
        {
            email,
            amount = amountMinor,
            currency,
            reference,
            callback_url = callbackUrl,
            metadata = new
            {
                payment_order_id = orderId,
                event_id = eventId,
                student_id = studentId
            }
        });
        using var response = await SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<PaystackEnvelope<InitializeData>>(
            cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload?.Status != true || payload.Data is null ||
            string.IsNullOrWhiteSpace(payload.Data.AuthorizationUrl))
        {
            logger.LogWarning(
                "Paystack rejected payment initialization for reference {Reference} with status {StatusCode}.",
                reference,
                (int)response.StatusCode);
            throw new PaymentProviderException("Paystack could not initialize the payment.");
        }
        return new PaymentProviderInitialization(payload.Data.AuthorizationUrl, payload.Data.Reference);
    }

    public async Task<PaymentProviderVerification> VerifyAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            $"/transaction/verify/{Uri.EscapeDataString(reference)}");
        using var response = await SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<PaystackEnvelope<VerifyData>>(
            cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode || payload?.Status != true || payload.Data is null)
            throw new PaymentProviderException("Paystack could not verify the payment.");
        return new PaymentProviderVerification(
            string.Equals(payload.Data.Status, "success", StringComparison.OrdinalIgnoreCase),
            payload.Data.Reference,
            payload.Data.Amount,
            payload.Data.Currency.ToUpperInvariant());
    }

    public async Task<bool> RequestRefundAsync(
        string reference,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "/refund");
        request.Content = JsonContent.Create(new { transaction = reference, amount = amountMinor });
        using var response = await SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<PaystackEnvelope<JsonElement>>(
            cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode && payload?.Status == true;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetSecret());
        return request;
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        httpClientFactory.CreateClient(nameof(PaystackPaymentProvider))
            .SendAsync(request, cancellationToken);

    private string GetSecret()
    {
        var secret = configuration["PAYSTACK_SECRET_KEY"];
        if (string.IsNullOrWhiteSpace(secret))
            secret = configuration["Payments:Paystack:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new PaymentProviderException("Paystack is not configured.");
        return secret;
    }

    private sealed record PaystackEnvelope<T>(bool Status, string Message, T? Data);
    private sealed record InitializeData(
        [property: System.Text.Json.Serialization.JsonPropertyName("authorization_url")] string AuthorizationUrl,
        [property: System.Text.Json.Serialization.JsonPropertyName("access_code")] string AccessCode,
        string Reference);
    private sealed record VerifyData(string Status, string Reference, long Amount, string Currency);
}

public sealed class PaymentProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);
