using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Search;

public sealed class When_ValidatingSearchRequest_WithConfiguredLimit_Should
{
    [Fact]
    public void RejectValueAboveConfiguredLimit()
    {
        var validator = new SearchLocationsRequestValidator(Options.Create(new LocationResolutionOptions
        {
            SupportedCountryCodes = ["NG"],
            MaxResults = 4
        }));

        var result = validator.Validate(new SearchLocationsRequest("Polo", Limit: 5));

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe("Limit");
    }
}
