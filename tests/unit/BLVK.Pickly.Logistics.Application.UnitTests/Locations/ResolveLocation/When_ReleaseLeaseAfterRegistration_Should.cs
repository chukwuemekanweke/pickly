
namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task ReleaseLeaseAfterRegistration()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate());

        await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        await context.Lease.Received(1).ReleaseAsync(
            LocationResolutionTestContext.ResolutionKey(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
