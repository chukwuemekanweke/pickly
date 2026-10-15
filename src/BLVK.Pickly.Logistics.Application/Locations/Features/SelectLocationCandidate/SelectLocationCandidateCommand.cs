using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;

public sealed record SelectLocationCandidateCommand(string SelectionToken, ActorContext ActorContext);
