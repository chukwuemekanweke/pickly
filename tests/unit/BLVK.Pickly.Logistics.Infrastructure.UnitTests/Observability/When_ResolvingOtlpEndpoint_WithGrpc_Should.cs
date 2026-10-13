using BLVK.Pickly.Logistics.Infrastructure.Observability;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Observability;

public sealed class When_ResolvingOtlpEndpoint_WithGrpc_Should
{
    [Fact]
    public void PreserveCollectorEndpoint()
    {
        var result = OtlpEndpointResolver.Resolve("http://otel-collector:4317", "grpc", "traces");

        result.ShouldBe(new Uri("http://otel-collector:4317"));
    }
}
