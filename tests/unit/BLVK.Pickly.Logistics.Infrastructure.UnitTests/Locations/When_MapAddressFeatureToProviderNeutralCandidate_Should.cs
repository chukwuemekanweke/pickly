using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
[Fact]
    public async Task MapAddressFeatureToProviderNeutralCandidate()
    {
        var context = new MapboxGeocodingTestContext();

        var result = await context.CreateProvider()
            .ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.Success);
        var candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.FeatureId.ShouldBe("s");
        candidate.FeatureType.ShouldBe("street");
        candidate.Precision.ShouldBe(GeocodingPrecision.Street);
        candidate.Confidence.ShouldBe(GeocodingMatchConfidence.Exact);
        candidate.DisplayAddress.ShouldBe("Ekpulechi Avenue, Enugu");
        candidate.AddressLine.ShouldBe("Ekpulechi Avenue");
        candidate.City.ShouldBe("Enugu");
        candidate.State.ShouldBe("Enugu");
        candidate.CountryCode.ShouldBe("NG");
        candidate.Latitude.ShouldBe(6.4512m);
        candidate.Longitude.ShouldBe(7.5221m);
    }
}
