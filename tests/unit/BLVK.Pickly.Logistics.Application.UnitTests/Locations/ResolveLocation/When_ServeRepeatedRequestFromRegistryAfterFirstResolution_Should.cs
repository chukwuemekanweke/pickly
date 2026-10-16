using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task ServeRepeatedRequestFromRegistryAfterFirstResolution()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate());
        var handler = context.ResolveHandler();

        var first = await handler.HandleAsync(context.Command(), CancellationToken.None);
        var second = await handler.HandleAsync(context.Command(), CancellationToken.None);

        second.FromRegistry.ShouldBeTrue();
        second.Location!.LocationId.ShouldBe(first.Location!.LocationId);
        await context.ProviderCalls(1);
    }
}
