namespace BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTransactionsCursorRequest(
    Guid StakeholderId,
    DateTimeOffset? CursorCreatedAtUtc,
    Guid? CursorTransactionId,
    int Limit);
