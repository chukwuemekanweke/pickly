using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
    private readonly ILocationReadModelRepository _repository = Substitute.For<ILocationReadModelRepository>();
    private readonly ActorContext _actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");

    [Fact]
    public async Task SearchRegistryWithSanitizedTermsAndReturnSummaries()
    {
        LocationSearchRequest? captured = null;
        var location = new LocationReadModel(
            Guid.CreateVersion7(),
            "Ekpulechi Avenue, Thinkers Corner, Enugu",
            "Ekpulechi Avenue",
            "Enugu",
            "Enugu",
            "NG",
            6.45m,
            7.52m,
            LocationType.Address,
            LocationResolutionSource.Mapbox,
            LocationResolutionStatus.Resolved,
            LocationVisibility.Public);
        _repository.SearchAsync(Arg.Do<LocationSearchRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns([location]);

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("Ekpulechi  Avenue:* | ' OR 1=1", "ng", 10, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(SearchLocationsStatus.Success);
        result.Items.ShouldHaveSingleItem().Id.ShouldBe(location.Id);
        captured.ShouldNotBeNull();
        captured.Terms.ShouldBe(["ekpulechi", "avenue", "or", "1"]);
        captured.CountryCode.ShouldBe("NG");
    }

    private SearchLocationsHandler Handler() => new(_repository, Options.Create(new LocationResolutionOptions()));
}
