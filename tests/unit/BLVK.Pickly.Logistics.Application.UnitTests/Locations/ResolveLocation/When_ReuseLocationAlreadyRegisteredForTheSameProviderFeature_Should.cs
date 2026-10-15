using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task ReuseLocationAlreadyRegisteredForTheSameProviderFeature()
    {
        var context = new LocationResolutionTestContext();
        var existing = context.SeedProviderLocation("Plot 12, Ekpulechi Av.", "address.12");
        context.ProviderReturns(LocationResolutionTestContext.Candidate(featureId: "address.12"));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Location!.LocationId.ShouldBe(existing.Id);
        context.LocationItems.ShouldHaveSingleItem();
        existing.ResolutionKeys.ShouldHaveSingleItem();
    }
}
