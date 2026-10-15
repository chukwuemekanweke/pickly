using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Fact]
    public async Task ReturnBadRequestForInvalidPayloadWithoutCallingProvider()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Resolve(
            new ResolveLocationRequest("", "Enugu", "Enugu", "NGA"),
            CancellationToken.None);

        var errors = result.Result.ShouldBeOfType<BadRequestObjectResult>().Value
            .ShouldBeOfType<ValidationProblemDetails>().Errors;
        errors.Keys.ShouldContain(nameof(ResolveLocationRequest.AddressLine));
        errors.Keys.ShouldContain(nameof(ResolveLocationRequest.CountryCode));
        await context.GeocodingProvider.DidNotReceive()
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }
}
