using BLVK.Pickly.Logistics.Domain.Locations.Services;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task RequestGeocodingWithNormalizedCountryAndBoundedCandidates()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate());

        await context.ResolveHandler().HandleAsync(context.Command(countryCode: "ng"), CancellationToken.None);

        await context.GeocodingProvider.Received(1).ForwardGeocodeAsync(
            Arg.Is<GeocodingRequest>(request =>
                request.CountryCode == "NG" &&
                request.AddressLine == LocationResolutionTestContext.AddressLine &&
                request.MaxCandidates == context.Settings.MaxProviderCandidates),
            Arg.Any<CancellationToken>());
    }
}
