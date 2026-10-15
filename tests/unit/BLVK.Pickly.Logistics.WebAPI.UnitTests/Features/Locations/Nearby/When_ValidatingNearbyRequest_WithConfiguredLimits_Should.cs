using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Nearby;

public sealed class When_ValidatingNearbyRequest_WithConfiguredLimits_Should
{
    [Fact]
    public void RejectValuesAboveConfiguredLimits()
    {
        var validator = new GetNearbyLocationsRequestValidator(Options.Create(new LocationResolutionOptions
        {
            MaxNearbyRadiusMeters = 750,
            MaxResults = 4
        }));

        var result = validator.Validate(new GetNearbyLocationsRequest(Guid.CreateVersion7(), 751, 5));

        result.Errors.Select(error => error.PropertyName).ShouldBe(["RadiusMeters", "Limit"], ignoreOrder: true);
    }
}
