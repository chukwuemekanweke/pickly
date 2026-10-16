using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithRegistryMatch_Should
{
[Fact]
    public async Task NotMatchDifferentPremiseOnTheSameStreet()
    {
        var context = new LocationResolutionTestContext();
        context.SeedProviderLocation("14 Ekpulechi Avenue", "address.14");
        context.ProviderReturns(LocationResolutionTestContext.Candidate());

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        result.FromRegistry.ShouldBeFalse();
        context.LocationItems.Count.ShouldBe(2);
        await context.ProviderCalls(1);
    }
}
