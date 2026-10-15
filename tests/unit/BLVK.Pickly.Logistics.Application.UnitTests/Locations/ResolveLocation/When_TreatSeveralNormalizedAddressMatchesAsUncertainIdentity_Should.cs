using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithRegistryMatch_Should
{
[Fact]
    public async Task TreatSeveralNormalizedAddressMatchesAsUncertainIdentity()
    {
        var context = new LocationResolutionTestContext();
        context.SeedProviderLocation(featureId: "address.12.a");
        context.SeedProviderLocation(featureId: "address.12.b");
        context.ProviderReturns();

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Unresolved);
        await context.ProviderCalls(1);
    }
}
