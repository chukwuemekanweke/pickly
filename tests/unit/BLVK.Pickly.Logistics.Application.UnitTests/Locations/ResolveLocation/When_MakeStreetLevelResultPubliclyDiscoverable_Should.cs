using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task MakeStreetLevelResultPubliclyDiscoverable()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate(
            featureId: "street.ekpulechi",
            addressLine: "Ekpulechi Avenue",
            precision: GeocodingPrecision.Street));

        await context.ResolveHandler().HandleAsync(context.Command("Ekpulechi Avenue"), CancellationToken.None);

        context.LocationItems.ShouldHaveSingleItem().Visibility.ShouldBe(LocationVisibility.Public);
    }
}
