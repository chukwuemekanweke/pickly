using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure;

public static class ForwardedHeadersConfiguration
{
    public static ForwardedHeadersOptions Create(IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1
        };

        foreach (var value in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
        {
            if (!System.Net.IPNetwork.TryParse(value, out var network))
            {
                throw new InvalidOperationException($"ForwardedHeaders:KnownNetworks contains invalid network '{value}'.");
            }

            options.KnownIPNetworks.Add(network);
        }

        foreach (var value in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(value, out var proxy))
            {
                throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains invalid address '{value}'.");
            }

            options.KnownProxies.Add(proxy);
        }

        return options;
    }
}
