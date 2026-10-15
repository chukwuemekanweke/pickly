using BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetNearbyLocations;

public sealed partial class When_GettingNearbyLocations_WithAnchorLocation_Should
{
    private readonly ILocationReadModelRepository _repository = Substitute.For<ILocationReadModelRepository>();
    private readonly ActorContext _actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");

    [Fact]
    public async Task QueryAroundAnchorExcludingItAndBoundingRadiusAndLimit()
    {
        var anchor = ReadModel(Guid.CreateVersion7(), LocationType.Address);
        var nearby = new NearbyLocationReadModel(ReadModel(Guid.CreateVersion7(), LocationType.Landmark), 245.6);
        _repository.GetByIdAsync(anchor.Id, Arg.Any<CancellationToken>()).Returns(anchor);
        NearbyLocationsRequest? captured = null;
        _repository.FindNearbyAsync(Arg.Do<NearbyLocationsRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns([nearby]);

        var result = await Handler().HandleAsync(
            new GetNearbyLocationsQuery(anchor.Id, 999_999, 999, LocationType.Landmark, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(GetNearbyLocationsStatus.Success);
        var item = result.Items.ShouldHaveSingleItem();
        item.Id.ShouldBe(nearby.Location.Id);
        item.DistanceMeters.ShouldBe(246);
        captured.ShouldNotBeNull();
        captured.ExcludeLocationId.ShouldBe(anchor.Id);
        captured.Anchor.Latitude.ShouldBe(anchor.Latitude);
        captured.Anchor.Longitude.ShouldBe(anchor.Longitude);
        captured.RadiusMeters.ShouldBe(new LocationResolutionOptions().MaxNearbyRadiusMeters);
        captured.Limit.ShouldBe(new LocationResolutionOptions().MaxResults);
        captured.LocationType.ShouldBe(LocationType.Landmark);
    }

    private GetNearbyLocationsHandler Handler() => new(_repository, Options.Create(new LocationResolutionOptions()));

    private static LocationReadModel ReadModel(Guid id, LocationType type) =>
        new(
            id,
            "Polo Park Mall, Enugu",
            "Polo Park Mall",
            "Enugu",
            "Enugu",
            "NG",
            6.4420m,
            7.5005m,
            type,
            LocationResolutionSource.InternallyRegistered,
            LocationResolutionStatus.Resolved,
            LocationVisibility.Public);
}
