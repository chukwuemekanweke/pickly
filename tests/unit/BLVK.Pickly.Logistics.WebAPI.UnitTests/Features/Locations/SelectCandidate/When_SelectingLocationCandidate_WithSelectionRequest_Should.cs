using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.SelectCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionRequest_Should
{
    [Fact]
    public async Task ReturnResolvedLocationForStoredSelection()
    {
        var context = new LocationsControllerTestContext();
        var selectionId = LocationSelectionTokens.NewSelectionId();
        context.Cache.GetAsync<PendingLocationSelection>(
                LocationSelectionTokens.SelectionCacheKey(selectionId),
                Arg.Any<CancellationToken>())
            .Returns(new PendingLocationSelection(
                context.StakeholderId,
                "mapbox",
                LocationResolutionSource.Mapbox,
                [LocationsControllerTestContext.Candidate(
                    precision: GeocodingPrecision.Street,
                    confidence: GeocodingMatchConfidence.Medium)]));

        var result = await context.CreateController().Register(
            new RegisterLocationRequest(LocationSelectionTokens.Create(selectionId, 0)),
            CancellationToken.None);

        var response = result.Result.ShouldBeOfType<CreatedAtActionResult>().Value.ShouldBeOfType<LocationResponse>();
        response.LocationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task ReturnOkWhenRegistrationReusesCanonicalLocation()
    {
        var context = new LocationsControllerTestContext();
        var selectionId = LocationSelectionTokens.NewSelectionId();
        context.Cache.GetAsync<PendingLocationSelection>(
                LocationSelectionTokens.SelectionCacheKey(selectionId),
                Arg.Any<CancellationToken>())
            .Returns(new PendingLocationSelection(
                context.StakeholderId,
                "mapbox",
                LocationResolutionSource.Mapbox,
                [LocationsControllerTestContext.Candidate(precision: GeocodingPrecision.Street)]));
        context.RegistrationStore.RegisterAsync(
                Arg.Any<Location>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new LocationRegistration(call.ArgAt<Location>(0), false));

        var result = await context.CreateController().Register(
            new RegisterLocationRequest(LocationSelectionTokens.Create(selectionId, 0)),
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<LocationResponse>();
    }
}
