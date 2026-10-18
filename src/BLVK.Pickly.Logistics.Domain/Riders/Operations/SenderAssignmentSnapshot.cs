namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public sealed record SenderAssignmentSnapshot(
    string MatchingStatus,
    bool HasActiveOffer,
    Guid? RiderId,
    string? RiderName,
    string? RiderAvatarUrl,
    DateTimeOffset? AssignedAtUtc);
