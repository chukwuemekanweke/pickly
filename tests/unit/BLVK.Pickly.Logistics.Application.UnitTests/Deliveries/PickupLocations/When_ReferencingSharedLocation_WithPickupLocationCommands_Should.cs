using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
    private static readonly Type[] ResolutionDependencies =
    [
        typeof(ILocationGeocodingProvider),
        typeof(ILocationResolutionLease),
        typeof(ILocationRegistrationStore),
        typeof(LocationCandidateRegistrar),
        typeof(ResolveLocationHandler),
        typeof(HttpClient)
    ];

    [Fact]
    public async Task CreateAssociationReturningSharedLocationDetails()
    {
        var context = new PickupLocationHandlerTestContext();

        var result = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Home", additionalDirections: "Call when you arrive at the entrance"),
            CancellationToken.None);

        result.Status.ShouldBe(CreatePickupLocationStatus.Success);
        result.Location!.LocationId.ShouldBe(context.HomeLocation.Id);
        result.Location.AdditionalDirections.ShouldBe("Call when you arrive at the entrance");
        result.Location.Address.AddressLine.ShouldBe("36 Garden Avenue");
        result.Location.Address.LocationType.ShouldBe(LocationType.Address);
        result.Location.Address.Latitude.ShouldBe(context.HomeLocation.Coordinates.Latitude);
        context.Repository.Locations.ShouldHaveSingleItem().LocationId.ShouldBe(context.HomeLocation.Id);
    }
}
