using BLVK.Pickly.Logistics.Infrastructure.Observability;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Observability;

public sealed class When_ResolvingOtlpEndpoint_WithSignalPath_Should
{
    [Fact]
    public void AvoidDuplicatingSignalPath()
    {
        var result = OtlpEndpointResolver.Resolve("https://example.grafana.net/otlp/v1/logs", "http/protobuf", "logs");

        result.ShouldBe(new Uri("https://example.grafana.net/otlp/v1/logs"));
    }
}
