using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
    [Fact]
    public async Task RegisterProviderResultAndReturnNewLocationId()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate());

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        result.FromRegistry.ShouldBeFalse();
        var registered = context.LocationItems.ShouldHaveSingleItem();
        result.Location!.LocationId.ShouldBe(registered.Id);
        result.Location.ResolutionSource.ShouldBe(LocationResolutionSource.Mapbox);
        result.Location.Coordinates.Latitude.ShouldBe(6.4512m);
        result.Location.Coordinates.Longitude.ShouldBe(7.5221m);
        registered.Visibility.ShouldBe(LocationVisibility.Public);
        registered.ResolutionKeys.ShouldHaveSingleItem().Key.ShouldBe(LocationResolutionTestContext.ResolutionKey());
    }
}
