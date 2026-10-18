namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public sealed record RiderOperationResult(RiderOperationCode Code);

public sealed record RiderStatusResult(RiderOperationCode Code, RiderStatusSnapshot? Status = null);

public sealed record RiderOffersResult(
    RiderOperationCode Code,
    IReadOnlyCollection<RiderOfferSnapshot>? Offers = null);

public sealed record RiderOfferDecisionResult(
    RiderOperationCode Code,
    RiderAssignedDeliverySnapshot? Delivery = null);

public sealed record RiderActiveDeliveryResult(
    RiderOperationCode Code,
    RiderAssignedDeliverySnapshot? Delivery = null);

public sealed record MatchingProcessingResult(
    int ScheduledDispatchesOpened,
    int OffersExpired,
    int MatchingAttempts,
    int OffersCreated,
    int NoCandidateAttempts);
