using System.Net;
using System.Text;
using EventManagement.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManagement.Api.UnitTests.Services;

public sealed class FlutterwavePaymentProviderTests
{
    [Fact]
    public void Webhook_signature_requires_exact_configured_secret()
    {
        var provider = CreateProvider(_ => Json(HttpStatusCode.OK, "{}"));

        Assert.True(provider.HasValidSignature("{\"event\":\"charge.completed\"}", "sandbox-webhook-secret"));
        Assert.False(provider.HasValidSignature("{\"event\":\"charge.completed\"}", "wrong-secret"));
        Assert.False(provider.HasValidSignature("{\"event\":\"charge.completed\"}", null));
    }

    [Fact]
    public async Task Verify_uses_reference_endpoint_and_converts_major_units()
    {
        HttpRequestMessage? captured = null;
        var provider = CreateProvider(request =>
        {
            captured = request;
            return Json(HttpStatusCode.OK,
                """{"status":"success","message":"verified","data":{"id":42,"status":"successful","tx_ref":"ems_test","amount":125.50,"currency":"ghs"}}""");
        });

        var result = await provider.VerifyAsync("ems_test", CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.Equal("ems_test", result.Reference);
        Assert.Equal(12_550, result.AmountMinor);
        Assert.Equal("GHS", result.Currency);
        Assert.Equal("https://api.flutterwave.com/v3/transactions/verify_by_reference?tx_ref=ems_test", captured!.RequestUri!.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
        Assert.Equal("FLWSECK_TEST-sandbox", captured.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Verify_maps_provider_failure_to_payment_provider_exception()
    {
        var provider = CreateProvider(_ => Json(HttpStatusCode.BadGateway,
            """{"status":"error","message":"temporarily unavailable","data":null}"""));

        await Assert.ThrowsAsync<PaymentProviderException>(
            () => provider.VerifyAsync("ems_failed", CancellationToken.None));
    }

    private static FlutterwavePaymentProvider CreateProvider(
        Func<HttpRequestMessage, HttpResponseMessage> response)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payments:Flutterwave:SecretKey"] = "FLWSECK_TEST-sandbox",
            ["Payments:Flutterwave:WebhookSecret"] = "sandbox-webhook-secret"
        }).Build();
        return new FlutterwavePaymentProvider(
            configuration,
            new StubHttpClientFactory(new HttpClient(new StubHandler(response))),
            NullLogger<FlutterwavePaymentProvider>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
