namespace BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;

public sealed record SelectLocationCandidateResult(
    SelectLocationCandidateStatus Status,
    LocationResponse? Location = null,
    bool Created = false);
