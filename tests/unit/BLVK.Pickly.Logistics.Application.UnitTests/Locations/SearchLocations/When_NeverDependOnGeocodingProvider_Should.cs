using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
[Fact]
    public void NeverDependOnGeocodingProvider()
    {
        var dependencies = typeof(SearchLocationsHandler).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType);

        dependencies.ShouldNotContain(typeof(ILocationGeocodingProvider));
    }
}
