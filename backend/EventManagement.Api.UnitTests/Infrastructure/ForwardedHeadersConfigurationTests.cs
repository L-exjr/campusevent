using System.Net;
using EventManagement.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace EventManagement.Api.UnitTests.Infrastructure;

public sealed class ForwardedHeadersConfigurationTests
{
    [Fact]
    public void Trusts_railway_internal_proxy_network_without_trusting_arbitrary_clients()
    {
        var options = new ForwardedHeadersOptions();

        ForwardedHeadersConfiguration.Configure(options);

        Assert.Contains(options.KnownIPNetworks, network =>
            network.Contains(IPAddress.Parse("100.20.30.40")));
        Assert.DoesNotContain(options.KnownIPNetworks, network =>
            network.Contains(IPAddress.Parse("203.0.113.10")));
        Assert.Equal("X-Real-IP", options.ForwardedForHeaderName);
        Assert.Equal(1, options.ForwardLimit);
    }
}
