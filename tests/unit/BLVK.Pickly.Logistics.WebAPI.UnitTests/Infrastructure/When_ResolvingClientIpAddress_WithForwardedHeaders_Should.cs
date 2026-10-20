using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Infrastructure;

public sealed class When_ResolvingClientIpAddress_WithForwardedHeaders_Should
{
    [Fact]
    public void NormalizeIpv4MappedAddressAndTrustConfiguredProxyNetwork()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.10");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:ForwardLimit"] = "1",
                ["ForwardedHeaders:KnownNetworks:0"] = "172.16.0.0/12"
            })
            .Build();

        var options = ForwardedHeadersConfiguration.Create(configuration);

        context.GetClientIpAddress().ShouldBe("203.0.113.10");
        options.ForwardLimit.ShouldBe(1);
        options.KnownIPNetworks.ShouldContain(IPNetwork.Parse("172.16.0.0/12"));
    }
}
