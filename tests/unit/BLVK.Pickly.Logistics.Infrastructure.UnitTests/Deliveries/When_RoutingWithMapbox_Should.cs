using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

public sealed class When_RoutingWithMapbox_Should
{
    [Fact]
    public async Task ParseMetresAndDuration()
    {
        var context = new MapboxDeliveryRoutingTestContext();
        var result = await context.CreateProvider().GetRouteAsync(MapboxDeliveryRoutingTestContext.Request(), CancellationToken.None);
        result.Status.ShouldBe(DeliveryRouteStatus.Success);
        result.DistanceMeters.ShouldBe(8400);
        result.Duration.ShouldBe(TimeSpan.FromSeconds(1200.5));
    }

    [Fact]
    public async Task ReportNoRouteWithoutInventingDistance()
    {
        var context = new MapboxDeliveryRoutingTestContext { Responder = _ => MapboxDeliveryRoutingTestContext.Json("""{"code":"NoRoute","routes":[]}""") };
        var result = await context.CreateProvider().GetRouteAsync(MapboxDeliveryRoutingTestContext.Request(), CancellationToken.None);
        result.Status.ShouldBe(DeliveryRouteStatus.NoRoute);
        result.DistanceMeters.ShouldBe(0);
    }

    [Fact]
    public async Task ReuseScalarResultForSameDirectionOnly()
    {
        var context = new MapboxDeliveryRoutingTestContext();
        var provider = context.CreateProvider();
        var origin = Guid.CreateVersion7();
        var destination = Guid.CreateVersion7();
        await provider.GetRouteAsync(MapboxDeliveryRoutingTestContext.Request(origin, destination), CancellationToken.None);
        await provider.GetRouteAsync(MapboxDeliveryRoutingTestContext.Request(origin, destination), CancellationToken.None);
        await provider.GetRouteAsync(MapboxDeliveryRoutingTestContext.Request(destination, origin), CancellationToken.None);
        context.Requests.Count.ShouldBe(2);
    }
}
