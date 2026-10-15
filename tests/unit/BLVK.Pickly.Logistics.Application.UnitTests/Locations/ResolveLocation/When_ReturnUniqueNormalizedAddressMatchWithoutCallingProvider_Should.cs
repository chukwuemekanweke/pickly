using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithRegistryMatch_Should
{
[Fact]
    public async Task ReturnUniqueNormalizedAddressMatchWithoutCallingProvider()
    {
        var context = new LocationResolutionTestContext();
        var existing = context.SeedProviderLocation();

        var result = await context.ResolveHandler().HandleAsync(
            context.Command("12, ekpulechi  AVENUE"),
            CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        result.Location!.LocationId.ShouldBe(existing.Id);
        await context.ProviderCalls(0);
    }
}
