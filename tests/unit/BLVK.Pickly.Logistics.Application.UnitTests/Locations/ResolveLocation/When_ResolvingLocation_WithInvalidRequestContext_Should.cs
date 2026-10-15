using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithInvalidRequestContext_Should
{
    [Fact]
    public async Task RejectUnauthenticatedActor()
    {
        var context = new LocationResolutionTestContext();

        var result = await context.ResolveHandler().HandleAsync(
            context.Command() with { ActorContext = context.Actor with { StakeholderId = null } },
            CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.NotAuthenticated);
        await context.ProviderCalls(0);
    }
}
