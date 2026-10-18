namespace BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;

public sealed record ProcessRiderMatchingResult(
    int ScheduledDispatchesOpened,
    int OffersExpired,
    int MatchingAttempts,
    int OffersCreated,
    int NoCandidateAttempts);
