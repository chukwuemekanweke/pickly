namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface ISenderDeliveryAssignmentReader
{
    Task<SenderDeliveryAssignmentFacts> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
}

public sealed record SenderDeliveryAssignmentFacts(
    bool HasActiveOffer,
    bool HasActiveMatchingSession,
    bool IsMatchingEligible,
    Guid? RiderId,
    string? RiderName,
    string? RiderAvatarUrl,
    DateTimeOffset? AssignedAtUtc);
