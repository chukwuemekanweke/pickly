using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

public sealed class When_ResolvingRoutingWindows_Should
{
    [Theory]
    [InlineData(8, "early")]
    [InlineData(10, "morning")]
    [InlineData(12, "afternoon")]
    [InlineData(16, "evening-peak")]
    [InlineData(19, "night")]
    [InlineData(23, "late")]
    public void SelectConfiguredLocalWindow(int localHour, string expected)
    {
        var options = Options();
        var departure = new DateTimeOffset(2026, 10, 16, localHour - 1, 0, 0, TimeSpan.Zero);

        RoutingWindowResolver.Resolve(departure, options).Id.ShouldBe(expected);
    }

    [Fact]
    public void IncludeWindowScheduleAndLocalDateInCacheKey()
    {
        var request = new DeliveryRouteRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"), GeographicCoordinates.FromDegrees(6.4, 7.5), Guid.Parse("22222222-2222-2222-2222-222222222222"), GeographicCoordinates.FromDegrees(6.5, 7.6), "driving", new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        var window = RoutingWindowResolver.Resolve(request.DepartureAtUtc, Options());

        var key = MapboxDeliveryRouteProvider.BuildCacheKey(request, "driving", window);

        key.ShouldBe("delivery-route:v2:mapbox:11111111111111111111111111111111:22222222222222222222222222222222:driving:v1:morning:20261016");
    }

    [Fact]
    public void RejectRetentionBeyondTwentyFourHours()
    {
        Should.Throw<InvalidOperationException>(() => new DeliveryRoutingOptions { MaximumCacheAge = TimeSpan.FromHours(25), Windows = Options().Windows }.Validate());
    }

    [Fact]
    public void RejectMissingCoverage()
    {
        var options = new DeliveryRoutingOptions
        {
            Windows = [new() { Id = "morning", Start = TimeSpan.FromHours(9), End = TimeSpan.FromHours(12), ApplicableDays = Enum.GetValues<DayOfWeek>() }]
        };
        Should.Throw<InvalidOperationException>(options.Validate);
    }

    private static DeliveryRoutingOptions Options() => new()
    {
        Windows =
        [
            Window("early", 0, 9), Window("morning", 9, 12), Window("afternoon", 12, 15),
            Window("evening-peak", 15, 18), Window("night", 18, 21), Window("late", 21, 0)
        ]
    };

    private static RoutingWindowOptions Window(string id, int start, int end) => new()
    {
        Id = id,
        Start = TimeSpan.FromHours(start),
        End = TimeSpan.FromHours(end),
        ApplicableDays = Enum.GetValues<DayOfWeek>()
    };
}
