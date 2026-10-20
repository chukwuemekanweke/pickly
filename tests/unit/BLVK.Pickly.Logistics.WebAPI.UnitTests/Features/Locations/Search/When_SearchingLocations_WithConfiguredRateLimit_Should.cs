using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;
using System.Reflection;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Search;

public sealed class When_SearchingLocations_WithConfiguredRateLimit_Should
{
    [Fact]
    public void UseRelaxedSearchSpecificPolicy()
    {
        var attribute = typeof(LocationsController)
            .GetMethod(nameof(LocationsController.Search))!
            .GetCustomAttribute<EnableRateLimitingAttribute>();
        var options = new RateLimitingOptions();

        attribute.ShouldNotBeNull();
        attribute.PolicyName.ShouldBe(RateLimitingPolicyNames.LocationSearchPolicy);
        options.LocationSearchPolicy.PermitLimit.ShouldBe(100);
        options.LocationSearchPolicy.WindowMinutes.ShouldBe(1);
        options.LocationResolutionPolicy.PermitLimit.ShouldBe(20);
    }
}
