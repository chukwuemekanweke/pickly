namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal static class CredoTransactionStatuses
{
    public const int Successful = 0;
    public const int Refunded = 1;
    public const int Refund = 2;
    public const int Failed = 3;
    public const int Settle = 4;
    public const int Settled = 5;
    public const int Review = 6;
    public const int Declined = 7;
    public const int CancelledByCustomer = 9;
    public const int CancelledByMerchant = 10;
    public const int AccountGeneratedAwaitingCredit = 12;
    public const int Attempted = 13;
    public const int Initialized = 14;
    public const int Initializing = 15;
}
