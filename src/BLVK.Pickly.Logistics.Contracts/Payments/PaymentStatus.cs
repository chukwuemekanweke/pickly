namespace BLVK.Pickly.Logistics.Contracts.Payments;

public enum PaymentStatus
{
    PendingInitiation = 1,
    Initiated = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5,
    Expired = 6,
    Unresolved = 7
}
