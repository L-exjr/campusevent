using Microsoft.AspNetCore.HttpOverrides;

namespace EventManagement.Api.Infrastructure;

public static class ForwardedHeadersConfiguration
{
    // Railway documents its internal proxy network as 100.0.0.0/8. This shared
    // address range is not publicly routable, so direct internet clients cannot
    // qualify as a trusted forwarding hop merely by supplying proxy headers.
    private static readonly System.Net.IPNetwork RailwayProxyNetwork =
        System.Net.IPNetwork.Parse("100.0.0.0/8");

    public static void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Railway publishes X-Real-IP (rather than X-Forwarded-For) as the
        // original client address on its HTTP edge.
        options.ForwardedForHeaderName = "X-Real-IP";
        options.ForwardLimit = 1;
        // Keep ASP.NET Core's loopback defaults for local reverse proxies and add
        // only Railway's documented internal network for deployed traffic.
        options.KnownIPNetworks.Add(RailwayProxyNetwork);
    }
}
