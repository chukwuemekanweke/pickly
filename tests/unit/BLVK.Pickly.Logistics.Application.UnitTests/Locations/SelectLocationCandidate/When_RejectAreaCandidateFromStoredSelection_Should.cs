using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed class When_RejectAreaCandidateFromStoredSelection_Should
{
    [Fact]
    public async Task RejectWithoutRegistering()
    {
        var context = new LocationResolutionTestContext();
        var selectionId = LocationSelectionTokens.NewSelectionId();
        await context.Cache.SetAsync(
            LocationSelectionTokens.SelectionCacheKey(selectionId),
            new PendingLocationSelection(
                context.StakeholderId,
                "mapbox",
                LocationResolutionSource.Mapbox,
                [LocationResolutionTestContext.Candidate(precision: GeocodingPrecision.Area)]),
            context.Settings.CandidateSelectionLifetime,
            CancellationToken.None);

        var result = await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand(
                LocationSelectionTokens.Create(selectionId, 0),
                context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(SelectLocationCandidateStatus.LocationUnusable);
        await context.RegistrationStore.DidNotReceive().RegisterAsync(
            Arg.Any<Location>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
