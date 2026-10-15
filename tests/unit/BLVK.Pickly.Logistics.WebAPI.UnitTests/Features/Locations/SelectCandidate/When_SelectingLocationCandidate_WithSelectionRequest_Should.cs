using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
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
                [LocationsControllerTestContext.Candidate(confidence: GeocodingMatchConfidence.Medium)]));

        var result = await context.CreateController().SelectCandidate(
            new SelectLocationCandidateRequest(LocationSelectionTokens.Create(selectionId, 0)),
            CancellationToken.None);

        var response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ResolveLocationResponse>();
        response.Status.ShouldBe(ResolveLocationResponse.ResolvedStatus);
        response.LocationId.ShouldNotBeNull();
    }
}
