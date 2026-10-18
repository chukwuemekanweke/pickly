using BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;
using BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;
using BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Riders;

public sealed class When_HandlingRiderOperations_Should
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly ActorContext Actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");
    private static readonly RiderMatchingOptions MatchingOptions = new() { OfferTimeout = TimeSpan.FromSeconds(30) };

    public static TheoryData<RiderOperationCode, AcceptRiderOfferStatus> AcceptanceOutcomes => new()
    {
        { RiderOperationCode.Success, AcceptRiderOfferStatus.Success },
        { RiderOperationCode.NotAuthenticated, AcceptRiderOfferStatus.NotAuthenticated },
        { RiderOperationCode.NotRider, AcceptRiderOfferStatus.NotRider },
        { RiderOperationCode.Forbidden, AcceptRiderOfferStatus.Forbidden },
        { RiderOperationCode.NotFound, AcceptRiderOfferStatus.NotFound },
        { RiderOperationCode.Expired, AcceptRiderOfferStatus.Expired },
        { RiderOperationCode.Conflict, AcceptRiderOfferStatus.Conflict }
    };

    [Theory]
    [MemberData(nameof(AcceptanceOutcomes))]
    public async Task MapAcceptanceOutcomeAndUseInjectedClock(RiderOperationCode code, AcceptRiderOfferStatus expected)
    {
        var offerId = Guid.CreateVersion7();
        var persistence = Substitute.For<IRiderOfferAcceptancePersistence>();
        persistence.AcceptAsync(Actor, offerId, Now, MatchingOptions, Arg.Any<CancellationToken>())
            .Returns(new RiderOfferDecisionResult(code));
        var handler = new AcceptRiderOfferHandler(persistence, Options.Create(MatchingOptions), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(offerId, Actor), CancellationToken.None);

        result.Status.ShouldBe(expected);
    }

    [Fact]
    public async Task ReportInvalidLocationAsItsOwnStatus()
    {
        var capturedAt = Now.AddSeconds(-5);
        var persistence = Substitute.For<IRiderLocationPersistence>();
        persistence.UpdateAsync(Actor, 6.5m, 3.3m, capturedAt, Now, MatchingOptions, Arg.Any<CancellationToken>())
            .Returns(new RiderOperationResult(RiderOperationCode.InvalidLocation));
        var handler = new UpdateRiderLocationHandler(persistence, Options.Create(MatchingOptions), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(6.5m, 3.3m, capturedAt, Actor), CancellationToken.None);

        result.Status.ShouldBe(UpdateRiderLocationStatus.InvalidLocation);
    }

    [Fact]
    public async Task ReturnRiderStatusSnapshotWhenAvailabilityChanges()
    {
        var snapshot = new RiderStatusSnapshot(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Approved", "Available", true, true, Now, false, 0);
        var persistence = Substitute.For<IRiderAvailabilityPersistence>();
        persistence.SetAsync(Actor, true, Now, MatchingOptions, Arg.Any<CancellationToken>())
            .Returns(new RiderStatusResult(RiderOperationCode.Success, snapshot));
        var handler = new SetRiderAvailabilityHandler(persistence, Options.Create(MatchingOptions), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(true, Actor), CancellationToken.None);

        result.Status.ShouldBe(SetRiderAvailabilityStatus.Success);
        result.RiderStatus.ShouldBe(snapshot);
    }

    [Fact]
    public async Task RunMatchingWithInjectedClockAndConfiguredOptions()
    {
        var persistence = Substitute.For<IRiderMatchingPersistence>();
        persistence.ProcessAsync(Now, MatchingOptions, Arg.Any<CancellationToken>())
            .Returns(new MatchingProcessingResult(1, 2, 3, 4, 5));
        var handler = new ProcessRiderMatchingHandler(persistence, Options.Create(MatchingOptions), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(), CancellationToken.None);

        result.ShouldBe(new ProcessRiderMatchingResult(1, 2, 3, 4, 5));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
