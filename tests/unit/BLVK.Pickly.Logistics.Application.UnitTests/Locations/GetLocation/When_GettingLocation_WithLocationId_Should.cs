using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetLocation;

public sealed partial class When_GettingLocation_WithLocationId_Should
{
    private readonly ILocationReadModelRepository _repository = Substitute.For<ILocationReadModelRepository>();
    private readonly ActorContext _actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");

    [Fact]
    public async Task ReturnResolvedLocation()
    {
        var location = ReadModel(LocationResolutionStatus.Resolved);
        _repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await new GetLocationHandler(_repository).HandleAsync(
            new GetLocationQuery(location.Id, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(GetLocationStatus.Success);
        result.Location!.LocationId.ShouldBe(location.Id);
        result.Location.Coordinates.Latitude.ShouldBe(6.45m);
        result.Location.Coordinates.Longitude.ShouldBe(7.52m);
    }

    private static LocationReadModel ReadModel(LocationResolutionStatus status) =>
        new(
            Guid.CreateVersion7(),
            "Polo Park Mall, Enugu",
            "Polo Park Mall",
            "Enugu",
            "Enugu",
            "NG",
            6.45m,
            7.52m,
            LocationType.Landmark,
            LocationResolutionSource.InternallyRegistered,
            status,
            LocationVisibility.Public);
}
