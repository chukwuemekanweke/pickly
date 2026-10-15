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
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.Success);
        var candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.FeatureId.ShouldBe("dXJuOm1ieGFkcjoxMg");
        candidate.FeatureType.ShouldBe("address");
        candidate.Precision.ShouldBe(GeocodingPrecision.Premise);
        candidate.Confidence.ShouldBe(GeocodingMatchConfidence.Exact);
        candidate.DisplayAddress.ShouldBe("12 Ekpulechi Avenue, Enugu 400001, Enugu, Nigeria");
        candidate.AddressLine.ShouldBe("12 Ekpulechi Avenue");
        candidate.City.ShouldBe("Enugu");
        candidate.State.ShouldBe("Enugu");
        candidate.CountryCode.ShouldBe("NG");
        candidate.Latitude.ShouldBe(6.4512m);
        candidate.Longitude.ShouldBe(7.5221m);
    }
}
